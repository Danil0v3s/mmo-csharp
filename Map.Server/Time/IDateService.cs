namespace Map.Server.Time;

/// <summary>
/// rAthena <c>date.cpp</c> — server-side clock accessors used by
/// scripts (`date_get_*` BUILTIN), the day-of-week buff system
/// (Astrologian / Sun/Moon/Star auras), and time-of-day NPC
/// branches. All values come from <see cref="DateTime.Now"/> on the
/// host process; the in-game clock has no offset / acceleration.
/// </summary>
public interface IDateService
{
    /// <summary>rAthena <c>date_get_year</c>.</summary>
    int Year { get; }
    /// <summary>rAthena <c>date_get_month</c>.</summary>
    int Month { get; }
    /// <summary>rAthena <c>date_get_dayofmonth</c>.</summary>
    int DayOfMonth { get; }
    /// <summary>rAthena <c>date_get_dayofyear</c>.</summary>
    int DayOfYear { get; }
    /// <summary>rAthena <c>date_get_hour</c>.</summary>
    int Hour { get; }
    /// <summary>rAthena <c>date_get_min</c>.</summary>
    int Minute { get; }
    /// <summary>rAthena <c>date_get_sec</c>.</summary>
    int Second { get; }
    /// <summary>rAthena <c>date_get</c> — packed yyyymmdd integer.</summary>
    int GetDate();

    /// <summary>rAthena <c>is_day_of_sun</c> — day-of-week % 7 == 0 (Sun).</summary>
    bool IsSunDay { get; }
    /// <summary>rAthena <c>is_day_of_moon</c> — % 7 == 1 (Mon).</summary>
    bool IsMoonDay { get; }
    /// <summary>rAthena <c>is_day_of_star</c> — % 7 == 6 (Sat).</summary>
    bool IsStarDay { get; }
}
