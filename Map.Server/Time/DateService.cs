namespace Map.Server.Time;

/// <summary>
/// Default <see cref="IDateService"/>. Reads <see cref="DateTime.Now"/>
/// on every call so scripts always see the live host clock — there is
/// no caching window. Mirrors rAthena <c>date.cpp:18-150</c>.
/// </summary>
public sealed class DateService : IDateService
{
    public int Year => DateTime.Now.Year;
    public int Month => DateTime.Now.Month;
    public int DayOfMonth => DateTime.Now.Day;
    public int DayOfYear => DateTime.Now.DayOfYear;
    public int Hour => DateTime.Now.Hour;
    public int Minute => DateTime.Now.Minute;
    public int Second => DateTime.Now.Second;

    public int GetDate()
    {
        var n = DateTime.Now;
        return n.Year * 10000 + n.Month * 100 + n.Day;
    }

    // rAthena `is_day_of_*` uses gettime(GETTIME_WEEKDAY) (0=Sun, 6=Sat).
    public bool IsSunDay => DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
    public bool IsMoonDay => DateTime.Now.DayOfWeek == DayOfWeek.Monday;
    public bool IsStarDay => DateTime.Now.DayOfWeek == DayOfWeek.Saturday;
}
