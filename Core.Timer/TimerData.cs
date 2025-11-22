namespace Core.Timer;

/// <summary>
/// Timer types
/// </summary>
[Flags]
public enum TimerType : uint
{
    None = 0,
    OnceAutoDelete = 0x01,
    Interval = 0x02,
    RemoveHeap = 0x10,
}

/// <summary>
/// Timer function delegate
/// </summary>
/// <param name="timerId">Timer ID</param>
/// <param name="tick">Current tick when timer fires</param>
/// <param name="id">User-defined ID</param>
/// <param name="data">User-defined data</param>
/// <returns>Status code</returns>
public delegate int TimerFunc(int timerId, long tick, int id, nint data);

/// <summary>
/// Internal timer data structure
/// </summary>
public struct TimerData
{
    public long Tick;
    public TimerFunc? Func;
    public TimerType Type;
    public int Interval;
    public int Id;
    public nint Data;
}

