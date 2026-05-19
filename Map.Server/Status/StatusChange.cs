namespace Map.Server.Status;

/// <summary>
/// One live status-change instance on an entity. Mirrors rAthena
/// <c>status_change_entry</c> (status.hpp:3426): four val slots
/// (per-effect meaning) plus a duration timer.
/// </summary>
public sealed class StatusChange
{
    public required StatusType Type { get; init; }
    public int Val1;
    public int Val2;
    public int Val3;
    public int Val4;

    /// <summary>Absolute tick (<see cref="Environment.TickCount64"/>) at which this SC expires. -1 = permanent until removed.</summary>
    public long ExpiresAt;

    /// <summary>Tick of the next periodic (DoT / HoT) trigger. 0 = no periodic.</summary>
    public long NextTick;

    /// <summary>Period (ms) between periodic ticks. 0 = no periodic.</summary>
    public int PeriodMs;
}
