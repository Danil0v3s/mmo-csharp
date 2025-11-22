namespace Core.Timer;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public sealed record SchedulerMetrics
{
    public long ActiveTimers { get; init; }
    public long TotalScheduled { get; init; }
    public long TotalFired { get; init; }
    public long TotalCancelled { get; init; }
}

public readonly record struct TimerId(long Value);

/// <summary>
/// Pure event-driven scheduler - ZERO loops, ZERO polling, ZERO background threads.
/// Only wakes up when there's actual work to do.
/// </summary>
public sealed class Scheduler : IDisposable
{
    public static readonly TimeSpan Infinite = Timeout.InfiniteTimeSpan;
    public static readonly TimerId InvalidTimer = new(-1);

    private const int InstanceBits = 10;
    private const int TokenBits = 16;
    private const int LocalBits = 64 - InstanceBits - TokenBits;
    private const long LocalMask = (1L << LocalBits) - 1;
    private const long TokenMask = (1L << TokenBits) - 1;

    private static readonly object ConfigureLock = new();
    private static Scheduler[] s_instances = CreateSchedulers(Math.Max(4, Environment.ProcessorCount));
    private static int s_rr;
    private static volatile bool s_configured;

    // High-resolution timing
    private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1000.0;
    private static readonly double MillisecondsPerTick = 1000.0 / Stopwatch.Frequency;

    private readonly int _instanceId;
    private readonly BinaryHeap<long> _heap = new(256);
    private readonly ConcurrentQueue<Op> _ops = new();
    private readonly Func<long, long, long> _comparison;
    private readonly object _lock = new();

    // Single timer for next wakeup - pure event driven
    private System.Threading.Timer? _nextWakeupTimer;
    private long _nextWakeupTick = long.MaxValue;

    private Entry[] _entries = new Entry[1024];
    private long _entryTop;
    private long _freeHead = -1;

    private long _metricsScheduled;
    private long _metricsFired;
    private long _metricsCancelled;
    private int _active;
    private volatile bool _disposed;

    static Scheduler()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                NativeMethods.TimeBeginPeriod(1);
            }
            catch { }
        }
    }

    private Scheduler(int instanceId)
    {
        this._instanceId = instanceId;
        this._comparison = this.CompareEntries;
    }

    public static void Configure(bool enableWindows1ms = true, int? minWorkerThreads = null, int? instanceCount = null)
    {
        if (s_configured && instanceCount is null && minWorkerThreads is null)
        {
            return;
        }

        lock (ConfigureLock)
        {
            if (instanceCount is { } count and > 0)
            {
                if (count != s_instances.Length)
                {
                    var newInstances = CreateSchedulers(count);
                    var oldInstances = Interlocked.Exchange(ref s_instances, newInstances);
                    foreach (var inst in oldInstances)
                    {
                        inst.Dispose();
                    }
                }
            }

            if (minWorkerThreads.HasValue)
            {
                ThreadPool.GetMinThreads(out var worker, out var io);
                ThreadPool.SetMinThreads(Math.Max(worker, minWorkerThreads.Value), io);
            }

            s_configured = true;
        }
    }

    public static TimerId Schedule(Func<object?, TimerId, long, ValueTask> asyncCallback, object? state, TimeSpan dueTime, bool flowExecutionContext = true)
    {
        if (TryDetectAbsoluteTicks(dueTime, out var scheduledTick))
        {
            return GetInstance().ScheduleAtTickInternal(asyncCallback, state, scheduledTick, Infinite, flowExecutionContext);
        }

        return GetInstance().ScheduleInternal(asyncCallback, state, dueTime, Infinite, flowExecutionContext, null);
    }

    public static TimerId SchedulePeriodic(Func<object?, TimerId, long, ValueTask> asyncCallback, object? state, TimeSpan dueTime, TimeSpan period, bool flowExecutionContext = true)
    {
        return GetInstance().ScheduleInternal(asyncCallback, state, dueTime, period, flowExecutionContext, null);
    }

    public static TimerId ScheduleAtTick(Func<object?, TimerId, long, ValueTask> asyncCallback, object? state, long scheduledTick, bool flowExecutionContext = true)
    {
        return GetInstance().ScheduleAtTickInternal(asyncCallback, state, scheduledTick, Infinite, flowExecutionContext);
    }

    public static void Cancel(TimerId handle)
    {
        var instance = HandleToInstance(handle);
        var token = HandleToToken(handle);
        var localId = HandleToLocal(handle);

        if ((uint)instance < (uint)s_instances.Length)
        {
            s_instances[instance].EnqueueCancel(localId, token);
        }
    }

    public static long ActiveCount
    {
        get
        {
            long total = 0;
            foreach (var inst in s_instances)
            {
                total += Volatile.Read(ref inst._active);
            }
            return total;
        }
    }

    public static SchedulerMetrics GetMetrics()
    {
        long active = 0, scheduled = 0, fired = 0, cancelled = 0;

        foreach (var inst in s_instances)
        {
            active += Volatile.Read(ref inst._active);
            scheduled += Volatile.Read(ref inst._metricsScheduled);
            fired += Volatile.Read(ref inst._metricsFired);
            cancelled += Volatile.Read(ref inst._metricsCancelled);
        }

        return new SchedulerMetrics
        {
            ActiveTimers = active,
            TotalScheduled = scheduled,
            TotalFired = fired,
            TotalCancelled = cancelled,
        };
    }

    public static void DisposeAll()
    {
        foreach (var inst in s_instances)
        {
            inst.Dispose();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                NativeMethods.TimeEndPeriod(1);
            }
            catch { }
        }
    }

    private static Scheduler GetInstance()
    {
        var array = s_instances;
        var idx = (int)((uint)Interlocked.Increment(ref s_rr) % (uint)array.Length);
        return array[idx];
    }

    private static Scheduler[] CreateSchedulers(int count)
    {
        var result = new Scheduler[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = new Scheduler(i);
        }
        return result;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long NowTick => Stopwatch.GetTimestamp();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long TimeSpanToTicks(TimeSpan span)
    {
        if (span == Infinite) return long.MaxValue;
        var ms = Math.Max(0, span.TotalMilliseconds);
        return (long)(ms * TicksPerMillisecond);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TicksToMilliseconds(long ticks)
    {
        var ms = ticks * MillisecondsPerTick;
        return ms <= 0 ? 1 : ms > int.MaxValue ? int.MaxValue : (int)ms;
    }

    private const long MaxAutoDetectDeltaMs = 120_000;

    private static bool TryDetectAbsoluteTicks(TimeSpan dueTime, out long scheduledTick)
    {
        if (dueTime == Infinite)
        {
            scheduledTick = 0;
            return false;
        }

        var candidate = dueTime.Ticks;

        if (candidate % TimeSpan.TicksPerMillisecond != 0)
        {
            var now = NowTick;
            var diff = candidate - now;
            var diffMs = diff * MillisecondsPerTick;

            if (candidate >= 0 && diffMs >= 0 && diffMs <= MaxAutoDetectDeltaMs)
            {
                scheduledTick = candidate;
                return true;
            }
        }

        scheduledTick = 0;
        return false;
    }

    private static TimerId MakeHandle(int instanceId, long token, long localId)
    {
        var value = (((long)instanceId & ((1 << InstanceBits) - 1)) << (TokenBits + LocalBits))
                    | ((token & TokenMask) << LocalBits)
                    | (localId & LocalMask);
        return new TimerId(value);
    }

    private static int HandleToInstance(TimerId handle) => (int)((ulong)handle.Value >> (TokenBits + LocalBits));
    private static long HandleToToken(TimerId handle) => (handle.Value >> LocalBits) & TokenMask;
    private static long HandleToLocal(TimerId handle) => handle.Value & LocalMask;

    private TimerId ScheduleInternal(
        Func<object?, TimerId, long, ValueTask> asyncCallback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period,
        bool flowExecutionContext,
        long? scheduledTickOverride)
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(Scheduler));
        }

        ArgumentNullException.ThrowIfNull(asyncCallback);

        var (localId, token) = this.AcquireEntry();

        var now = NowTick;
        var delayTicks = TimeSpanToTicks(dueTime);
        if (delayTicks == long.MaxValue)
        {
            delayTicks = 0;
        }

        var scheduledTick = scheduledTickOverride ?? (now + delayTicks);
        var dueTick = scheduledTick;
        var periodTicks = period == Infinite ? -1 : TimeSpanToTicks(period);
        var ec = flowExecutionContext ? ExecutionContext.Capture() : null;

        this._ops.Enqueue(Op.CreateAdd(localId, scheduledTick, dueTick, periodTicks, asyncCallback, state, ec));

        // PURE EVENT: Queue processing on ThreadPool
        ThreadPool.UnsafeQueueUserWorkItem(static s => ((Scheduler)s!).ProcessOps(), this, preferLocal: false);

        Interlocked.Increment(ref this._metricsScheduled);
        Interlocked.Increment(ref this._active);

        return MakeHandle(this._instanceId, token, localId);
    }

    private TimerId ScheduleAtTickInternal(
        Func<object?, TimerId, long, ValueTask> asyncCallback,
        object? state,
        long scheduledTick,
        TimeSpan period,
        bool flowExecutionContext)
    {
        var now = NowTick;
        var delayTicks = Math.Max(0, scheduledTick - now);
        var delayMs = delayTicks * MillisecondsPerTick;
        var dueTime = delayMs >= long.MaxValue ? Infinite : TimeSpan.FromMilliseconds(delayMs);

        return this.ScheduleInternal(asyncCallback, state, dueTime, period, flowExecutionContext, scheduledTick);
    }

    private void EnqueueCancel(long localId, long token)
    {
        this._ops.Enqueue(Op.CreateCancel(localId, token));
        ThreadPool.UnsafeQueueUserWorkItem(static s => ((Scheduler)s!).ProcessOps(), this, preferLocal: false);
    }

    // PURE EVENT-DRIVEN: Only runs when triggered
    private void ProcessOps()
    {
        if (this._disposed)
        {
            return;
        }

        lock (this._lock)
        {
            // Drain ALL ops first
            while (this._ops.TryDequeue(out var op))
            {
                switch (op.Kind)
                {
                    case OpKind.Add:
                        this.OnAdd(in op);
                        break;
                    case OpKind.Cancel:
                        this.OnCancel(in op);
                        break;
                    case OpKind.Finalize:
                        this.OnFinalize(in op);
                        break;
                }
            }

            // Fire ALL ready timers
            this.FireReadyTimers();

            // Always rearm - recalculate next wakeup
            this._nextWakeupTick = long.MaxValue;
            this.ArmNextWakeup();
        }
    }

    private void FireReadyTimers()
    {
        var now = NowTick;
        var fired = 0;

        while (this._heap.Length > 0)
        {
            var localId = this._heap.Peek();
            ref var entry = ref this._entries[localId];

            if (entry.DueTick > now)
            {
                break; // Not ready yet
            }

            this._heap.BHeapPop(this._comparison);
            entry.InHeap = 0;

            if (entry.Cancelled != 0 || entry.Callback is null)
            {
                this.FreeEntry(localId);
                continue;
            }

            entry.InFlight = 1;
            fired++;

            var handle = MakeHandle(this._instanceId, entry.Token, localId);
            var state = new CallbackState(
                this,
                localId,
                handle,
                entry.ScheduledTick,
                entry.Callback,
                entry.State,
                entry.ExecutionContext);

            ThreadPool.UnsafeQueueUserWorkItem(static s => DispatchCallback(s), state, preferLocal: false);
        }

        if (fired > 0)
        {
            Interlocked.Add(ref this._metricsFired, fired);
        }
    }

    private void ArmNextWakeup()
    {
        if (this._heap.Length == 0)
        {
            // No timers, disarm
            this._nextWakeupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            this._nextWakeupTick = long.MaxValue;
            return;
        }

        var localId = this._heap.Peek();
        var nextDue = this._entries[localId].DueTick;

        // Only rearm if this is earlier
        if (nextDue >= this._nextWakeupTick)
        {
            return;
        }

        this._nextWakeupTick = nextDue;
        var now = NowTick;
        var diff = nextDue - now;
        var delayMs = TicksToMilliseconds(diff);

        if (this._nextWakeupTimer == null)
        {
            this._nextWakeupTimer = new System.Threading.Timer(
                static s => ThreadPool.UnsafeQueueUserWorkItem(
                    static state => ((Scheduler)state!).ProcessOps(),
                    s,
                    preferLocal: false),
                this,
                delayMs,
                Timeout.Infinite);
        }
        else
        {
            this._nextWakeupTimer.Change(delayMs, Timeout.Infinite);
        }
    }

    private void OnAdd(in Op op)
    {
        ref var entry = ref this._entries[op.LocalId];
        entry.Callback = op.Callback;
        entry.State = op.State;
        entry.ExecutionContext = op.ExecutionContext;
        entry.DueTick = op.DueTick;
        entry.ScheduledTick = op.ScheduledTick;
        entry.PeriodTicks = op.PeriodTicks;
        entry.Cancelled = 0;
        entry.InFlight = 0;

        if (entry.InHeap == 0)
        {
            entry.InHeap = 1;
            this._heap.Ensure(1, 256);
            this._heap.BHeapPush(op.LocalId, this._comparison);
        }
    }

    private void OnCancel(in Op op)
    {
        if (op.LocalId >= this._entryTop)
        {
            return;
        }

        ref var entry = ref this._entries[op.LocalId];
        if (entry.Token == (ushort)op.Token && entry.Callback is not null)
        {
            entry.Cancelled = 1;
            Interlocked.Increment(ref this._metricsCancelled);
        }
    }

    private void OnFinalize(in Op op)
    {
        if (op.LocalId >= this._entryTop)
        {
            return;
        }

        ref var entry = ref this._entries[op.LocalId];
        entry.InFlight = 0;

        if (entry.Cancelled != 0 || entry.Callback is null)
        {
            this.FreeEntry(op.LocalId);
            return;
        }

        if (entry.PeriodTicks < 0)
        {
            this.FreeEntry(op.LocalId);
            return;
        }

        // Reschedule periodic timer
        entry.ScheduledTick += entry.PeriodTicks;
        entry.DueTick = entry.ScheduledTick;

        if (entry.InHeap == 0)
        {
            entry.InHeap = 1;
            this._heap.Ensure(1, 256);
            this._heap.BHeapPush(op.LocalId, this._comparison);
        }
    }

    private static void DispatchCallback(CallbackState state)
    {
        if (state.ExecutionContext is not null)
        {
            ExecutionContext.Run(state.ExecutionContext, static s => RunCallback((CallbackState)s!), state);
        }
        else
        {
            RunCallback(state);
        }
    }

#pragma warning disable VSTHRD100
    private static async void RunCallback(CallbackState state)
#pragma warning restore VSTHRD100
    {
        try
        {
            await state.Callback(state.State, state.Handle, state.ScheduledTick).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Scheduler] callback exception: {ex}");
        }
        finally
        {
            state.Scheduler._ops.Enqueue(Op.CreateFinalize(state.LocalId));
            ThreadPool.UnsafeQueueUserWorkItem(static s => ((Scheduler)s!).ProcessOps(), state.Scheduler, preferLocal: false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long CompareEntries(long lhs, long rhs)
    {
        return this._entries[lhs].DueTick - this._entries[rhs].DueTick;
    }

    private (long LocalId, long Token) AcquireEntry()
    {
        long id;
        if (this._freeHead >= 0)
        {
            id = this._freeHead;
            this._freeHead = this._entries[id].NextFree;
        }
        else
        {
            id = this._entryTop++;
            if (id >= this._entries.LongLength)
            {
                Array.Resize(ref this._entries, this._entries.Length * 2);
                for (var i = this._entries.Length / 2; i < this._entries.Length; i++)
                {
                    this._entries[i] = default;
                    this._entries[i].NextFree = -1;
                }
            }
        }

        ref var entry = ref this._entries[id];
        var token = (ushort)((entry.Token + 1) & 0xFFFF);
        if (token == 0)
        {
            token = 1;
        }

        entry.Token = token;
        entry.Cancelled = 0;
        entry.InHeap = 0;
        entry.InFlight = 0;
        entry.NextFree = -1;

        return (id, token);
    }

    private void FreeEntry(long localId)
    {
        ref var entry = ref this._entries[localId];
        entry.Callback = null;
        entry.State = null;
        entry.ExecutionContext = null;
        entry.Cancelled = 0;
        entry.InHeap = 0;
        entry.InFlight = 0;
        entry.DueTick = 0;
        entry.ScheduledTick = 0;
        entry.PeriodTicks = -1;

        entry.NextFree = this._freeHead;
        this._freeHead = localId;

        Interlocked.Decrement(ref this._active);
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        try
        {
            this._nextWakeupTimer?.Dispose();
        }
        catch { }

        while (this._ops.TryDequeue(out _))
        {
        }

        this._heap.Clear();
    }

    private readonly struct CallbackState
    {
        public readonly Scheduler Scheduler;
        public readonly long LocalId;
        public readonly TimerId Handle;
        public readonly long ScheduledTick;
        public readonly Func<object?, TimerId, long, ValueTask> Callback;
        public readonly object? State;
        public readonly ExecutionContext? ExecutionContext;

        public CallbackState(
            Scheduler scheduler,
            long localId,
            TimerId handle,
            long scheduledTick,
            Func<object?, TimerId, long, ValueTask> callback,
            object? state,
            ExecutionContext? executionContext)
        {
            this.Scheduler = scheduler;
            this.LocalId = localId;
            this.Handle = handle;
            this.ScheduledTick = scheduledTick;
            this.Callback = callback;
            this.State = state;
            this.ExecutionContext = executionContext;
        }
    }

    private readonly struct Op
    {
        public readonly OpKind Kind;
        public readonly long LocalId;
        public readonly long Token;
        public readonly long ScheduledTick;
        public readonly long DueTick;
        public readonly long PeriodTicks;
        public readonly Func<object?, TimerId, long, ValueTask>? Callback;
        public readonly object? State;
        public readonly ExecutionContext? ExecutionContext;

        private Op(
            OpKind kind,
            long localId,
            long token,
            long scheduledTick,
            long dueTick,
            long periodTicks,
            Func<object?, TimerId, long, ValueTask>? callback,
            object? state,
            ExecutionContext? executionContext)
        {
            this.Kind = kind;
            this.LocalId = localId;
            this.Token = token;
            this.ScheduledTick = scheduledTick;
            this.DueTick = dueTick;
            this.PeriodTicks = periodTicks;
            this.Callback = callback;
            this.State = state;
            this.ExecutionContext = executionContext;
        }

        public static Op CreateAdd(
            long localId,
            long scheduledTick,
            long dueTick,
            long periodTicks,
            Func<object?, TimerId, long, ValueTask> callback,
            object? state,
            ExecutionContext? executionContext)
            => new(OpKind.Add, localId, 0, scheduledTick, dueTick, periodTicks, callback, state, executionContext);

        public static Op CreateCancel(long localId, long token)
            => new(OpKind.Cancel, localId, token, 0, 0, -1, null, null, null);

        public static Op CreateFinalize(long localId)
            => new(OpKind.Finalize, localId, 0, 0, 0, -1, null, null, null);
    }

    private enum OpKind : byte
    {
        Add,
        Cancel,
        Finalize,
    }

    private struct Entry
    {
        public Func<object?, TimerId, long, ValueTask>? Callback;
        public object? State;
        public ExecutionContext? ExecutionContext;
        public long DueTick;
        public long ScheduledTick;
        public long PeriodTicks;
        public ushort Token;
        public byte Cancelled;
        public byte InHeap;
        public byte InFlight;
        public long NextFree;
    }

    private static class NativeMethods
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        internal static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        internal static extern uint TimeEndPeriod(uint uMilliseconds);
    }
}