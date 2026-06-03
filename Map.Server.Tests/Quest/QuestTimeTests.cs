using Map.Server.Quest;

namespace Map.Server.Tests.Quest;

/// <summary>
/// FEATURE-03 — rAthena TimeLimit parsing (quest_time / solve_time / split_exact_quest_time).
/// </summary>
public class QuestTimeTests
{
    private static readonly DateTimeOffset Base = new(2026, 6, 3, 2, 0, 0, TimeSpan.Zero); // 02:00 UTC, a Wednesday
    private static long BaseUnix => Base.ToUnixTimeSeconds();

    [Theory]
    [InlineData("+3h", 3 * 3600)]
    [InlineData("+1h", 3600)]
    [InlineData("+30mn", 30 * 60)]
    [InlineData("+5mn", 5 * 60)]
    [InlineData("+1d", 86400)]
    [InlineData("+7d", 7 * 86400)]
    [InlineData("+30s", 30)]
    [InlineData("+2h30mn", 2 * 3600 + 30 * 60)]
    public void Relative_durations_add_to_now(string limit, long expectedSeconds)
    {
        Assert.Equal(BaseUnix + expectedSeconds, QuestTime.ParseTimeUnix(limit, BaseUnix, Base));
    }

    [Fact]
    public void Empty_is_no_limit()
    {
        Assert.Equal(0, QuestTime.ParseTimeUnix("", BaseUnix, Base));
        Assert.Equal(0, QuestTime.ParseTimeUnix(null, BaseUnix, Base));
    }

    [Fact]
    public void Absolute_daily_before_reset_is_today()
    {
        // Base is 02:00; "4h" daily reset (04:00) is still ahead today → now + 2h.
        Assert.Equal(BaseUnix + 2 * 3600, QuestTime.ParseTimeUnix("4h", BaseUnix, Base));
    }

    [Fact]
    public void Absolute_daily_after_reset_is_tomorrow()
    {
        var sixAm = new DateTimeOffset(2026, 6, 3, 6, 0, 0, TimeSpan.Zero);
        var unix = sixAm.ToUnixTimeSeconds();
        // 06:00 is past today's 04:00 reset → next day 04:00 = now + 22h.
        Assert.Equal(unix + 22 * 3600, QuestTime.ParseTimeUnix("4h", unix, sixAm));
    }

    [Fact]
    public void Absolute_weekly_targets_the_named_weekday()
    {
        // Base is a Wednesday (wday 3). "Friday 4h" → this Friday 04:00.
        var expiry = QuestTime.ParseTimeUnix("Friday 4h", BaseUnix, Base);
        var dt = DateTimeOffset.FromUnixTimeSeconds(expiry).ToOffset(TimeSpan.Zero);
        Assert.Equal(DayOfWeek.Friday, dt.DayOfWeek);
        Assert.Equal(4, dt.Hour);
        Assert.True(expiry > BaseUnix);
    }
}
