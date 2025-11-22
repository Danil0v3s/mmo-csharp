using System.Diagnostics;

namespace Core.Timer;

/// <summary>
/// High-performance timer manager with zero-allocation design.
/// Based on rAthena timer system.
/// </summary>
public sealed class TimerManager
{
    public const int InvalidTimer = -1;
    public const long InfiniteTick = -1;
    public const long TimerMinInterval = 20;
    public const long TimerMaxInterval = 1000;

    private TimerData[] _timerData;
    private int _timerDataMax;
    private int _timerDataNum;

    private int[] _freeTimerList;
    private int _freeTimerListMax;
    private int _freeTimerListPos;

    private readonly TimerHeap _timerHeap;
    private readonly Dictionary<TimerFunc, string> _funcNames;
    private readonly Stopwatch _stopwatch;
    private readonly DateTime _startTime;

    // Tick caching
    private long _tickCache;
    private int _tickCacheCount;
    private const int TickCacheLimit = 10;

    public TimerManager(int initialCapacity = 256)
    {
        _timerDataMax = initialCapacity;
        _timerData = new TimerData[_timerDataMax];
        _timerDataNum = 0;

        _freeTimerListMax = initialCapacity;
        _freeTimerList = new int[_freeTimerListMax];
        _freeTimerListPos = 0;

        _timerHeap = new TimerHeap(_timerData, initialCapacity);
        _funcNames = new Dictionary<TimerFunc, string>();

        _stopwatch = Stopwatch.StartNew();
        _startTime = DateTime.UtcNow;
        _tickCache = 0;
        _tickCacheCount = 0;
    }

    /// <summary>
    /// Gets the current tick count in milliseconds
    /// </summary>
    public long GetTick()
    {
        if (--_tickCacheCount <= 0)
        {
            _tickCacheCount = TickCacheLimit;
            _tickCache = _stopwatch.ElapsedMilliseconds;
        }
        return _tickCache;
    }

    /// <summary>
    /// Gets the current tick count without caching
    /// </summary>
    public long GetTickNoCache()
    {
        _tickCacheCount = TickCacheLimit;
        _tickCache = _stopwatch.ElapsedMilliseconds;
        return _tickCache;
    }

    /// <summary>
    /// Gets the server uptime in seconds
    /// </summary>
    public long GetUptime()
    {
        return (long)(DateTime.UtcNow - _startTime).TotalSeconds;
    }

    /// <summary>
    /// Registers a timer function name for debugging purposes
    /// </summary>
    public void AddTimerFuncList(TimerFunc func, string name)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        if (_funcNames.ContainsKey(func))
        {
            Console.WriteLine($"Warning: Timer function {func.Method.Name} already registered as {_funcNames[func]}, re-registering as {name}");
            _funcNames[func] = name;
        }
        else
        {
            _funcNames[func] = name;
        }
    }

    /// <summary>
    /// Gets the name of a timer function
    /// </summary>
    public string SearchTimerFuncList(TimerFunc? func)
    {
        if (func == null)
            return "null function";

        return _funcNames.TryGetValue(func, out var name) ? name : func.Method.Name;
    }

    /// <summary>
    /// Adds a one-shot timer that is deleted once it expires
    /// </summary>
    /// <param name="tick">Tick when the timer should fire</param>
    /// <param name="func">Function to call</param>
    /// <param name="id">User-defined ID</param>
    /// <param name="data">User-defined data</param>
    /// <returns>Timer ID</returns>
    public int AddTimer(long tick, TimerFunc func, int id, nint data)
    {
        int tid = AcquireTimer();
        _timerData[tid].Tick = tick;
        _timerData[tid].Func = func;
        _timerData[tid].Id = id;
        _timerData[tid].Data = data;
        _timerData[tid].Type = TimerType.OnceAutoDelete;
        _timerData[tid].Interval = 1000;
        _timerHeap.Push(tid);
        return tid;
    }

    /// <summary>
    /// Adds an interval timer that automatically restarts itself
    /// </summary>
    /// <param name="tick">Tick when the timer should first fire</param>
    /// <param name="func">Function to call</param>
    /// <param name="id">User-defined ID</param>
    /// <param name="data">User-defined data</param>
    /// <param name="interval">Interval in milliseconds</param>
    /// <returns>Timer ID or InvalidTimer on failure</returns>
    public int AddTimerInterval(long tick, TimerFunc func, int id, nint data, int interval)
    {
        if (interval < 1)
        {
            Console.WriteLine($"Error: add_timer_interval: invalid interval (tick={tick} {SearchTimerFuncList(func)} id={id} data={data})");
            return InvalidTimer;
        }

        int tid = AcquireTimer();
        _timerData[tid].Tick = tick;
        _timerData[tid].Func = func;
        _timerData[tid].Id = id;
        _timerData[tid].Data = data;
        _timerData[tid].Type = TimerType.Interval;
        _timerData[tid].Interval = interval;
        _timerHeap.Push(tid);
        return tid;
    }

    /// <summary>
    /// Gets timer data for a given timer ID
    /// </summary>
    public TimerData? GetTimer(int tid)
    {
        if (tid < 0 || tid >= _timerDataNum)
            return null;
        return _timerData[tid];
    }

    /// <summary>
    /// Marks a timer for deletion
    /// </summary>
    /// <param name="tid">Timer ID</param>
    /// <param name="func">Function for verification</param>
    /// <returns>0 on success, negative on failure</returns>
    public int DeleteTimer(int tid, TimerFunc func)
    {
        if (tid < 0 || tid >= _timerDataNum)
        {
            Console.WriteLine($"Error: delete_timer: no such timer {tid} ({SearchTimerFuncList(func)})");
            return -1;
        }

        if (_timerData[tid].Func != func)
        {
            Console.WriteLine($"Error: delete_timer: function mismatch {SearchTimerFuncList(_timerData[tid].Func)} != {SearchTimerFuncList(func)}");
            return -2;
        }

        _timerData[tid].Func = null;
        _timerData[tid].Type = TimerType.OnceAutoDelete;
        return 0;
    }

    /// <summary>
    /// Adjusts a timer's expiration time by adding ticks
    /// </summary>
    public long AddTickTimer(int tid, long tick)
    {
        return SetTickTimer(tid, _timerData[tid].Tick + tick);
    }

    /// <summary>
    /// Modifies a timer's expiration time
    /// </summary>
    public long SetTickTimer(int tid, long tick)
    {
        int index = _timerHeap.FindIndex(tid);
        if (index == -1)
        {
            Console.WriteLine($"Error: settick_timer: no such timer {tid} ({SearchTimerFuncList(_timerData[tid].Func)})");
            return -1;
        }

        if (tick == -1)
            tick = 0; // avoid error value -1

        if (_timerData[tid].Tick == tick)
            return tick; // already in proper position

        // Remove from heap and re-insert with new tick
        _timerHeap.PopIndex(index);
        _timerData[tid].Tick = tick;
        _timerHeap.Push(tid);
        return tick;
    }

    /// <summary>
    /// Executes all expired timers
    /// </summary>
    /// <param name="tick">Current tick</param>
    /// <returns>Time until next timer expires (capped between min and max interval)</returns>
    public long DoTimer(long tick)
    {
        long diff = TimerMaxInterval;

        while (_timerHeap.Length > 0)
        {
            int tid = _timerHeap.Peek();
            diff = _timerData[tid].Tick - tick;

            if (diff > 0)
                break; // no more expired timers

            // Remove timer from heap
            _timerHeap.Pop();
            _timerData[tid].Type |= TimerType.RemoveHeap;

            var timerFunc = _timerData[tid].Func;
            if (timerFunc != null)
            {
                try
                {
                    if (diff < -1000)
                    {
                        // Timer delayed by more than 1 second, use current tick
                        timerFunc(tid, tick, _timerData[tid].Id, _timerData[tid].Data);
                    }
                    else
                    {
                        timerFunc(tid, _timerData[tid].Tick, _timerData[tid].Id, _timerData[tid].Data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: Timer function {SearchTimerFuncList(timerFunc)} threw exception: {ex}");
                }
            }

            // Handle timer post-execution
            if ((_timerData[tid].Type & TimerType.RemoveHeap) != 0)
            {
                _timerData[tid].Type &= ~TimerType.RemoveHeap;

                switch (_timerData[tid].Type)
                {
                    case TimerType.OnceAutoDelete:
                    default:
                        _timerData[tid].Type = TimerType.None;
                        if (_freeTimerListPos >= _freeTimerListMax)
                        {
                            _freeTimerListMax += 256;
                            Array.Resize(ref _freeTimerList, _freeTimerListMax);
                        }
                        _freeTimerList[_freeTimerListPos++] = tid;
                        break;

                    case TimerType.Interval:
                        if (_timerData[tid].Tick - tick < -1000)
                            _timerData[tid].Tick = tick + _timerData[tid].Interval;
                        else
                            _timerData[tid].Tick += _timerData[tid].Interval;
                        _timerHeap.Push(tid);
                        break;
                }
            }
        }

        return Math.Clamp(diff, TimerMinInterval, TimerMaxInterval);
    }

    /// <summary>
    /// Acquires a free timer ID
    /// </summary>
    private int AcquireTimer()
    {
        int tid;

        // Try to get a free timer from the list
        if (_freeTimerListPos > 0)
        {
            do
            {
                tid = _freeTimerList[--_freeTimerListPos];
            } while (tid >= _timerDataNum && _freeTimerListPos > 0);
        }
        else
        {
            tid = _timerDataNum;
        }

        // Check available space
        if (tid >= _timerDataNum)
        {
            for (tid = _timerDataNum; tid < _timerDataMax && _timerData[tid].Type != TimerType.None; tid++) ;
        }

        if (tid >= _timerDataNum && tid >= _timerDataMax)
        {
            // Expand timer array
            _timerDataMax += 256;
            Array.Resize(ref _timerData, _timerDataMax);
        }

        if (tid >= _timerDataNum)
            _timerDataNum = tid + 1;

        return tid;
    }

    /// <summary>
    /// Clears all timers and resets the manager
    /// </summary>
    public void Clear()
    {
        _timerHeap.Clear();
        Array.Clear(_timerData, 0, _timerDataNum);
        _timerDataNum = 0;
        _freeTimerListPos = 0;
        _funcNames.Clear();
    }
}

