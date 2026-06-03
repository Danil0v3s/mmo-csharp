using Map.Server.Agit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Agit;

/// <summary>
/// FEATURE-15 — WoE weekly scheduler: edge-triggered auto start/end, no per-tick re-fire, boot-inside
/// activation, independent editions, manual-override coexistence, and reload.
/// </summary>
public class WoeSchedulerTests
{
    // A fixed reference week: 2026-06-06 is a Saturday.
    private static DateTime Sat(int h, int m) => new(2026, 6, 6, h, m, 0, DateTimeKind.Local);
    private static DateTime Sun(int h, int m) => new(2026, 6, 7, h, m, 0, DateTimeKind.Local);

    private static (WoeScheduler Sched, AgitService Agit) Build(params WoeWindow[] windows)
    {
        var agit = new AgitService(NullLogger<AgitService>.Instance);
        var sched = new WoeScheduler(agit, NullLogger<WoeScheduler>.Instance);
        sched.SetWindows(windows);
        return (sched, agit);
    }

    private static WoeWindow Window(WoeEdition ed, DayOfWeek day, string start, string end)
        => new(ed, day, TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Fact]
    public void Auto_starts_at_open_and_ends_at_close_once_each()
    {
        var (s, agit) = Build(Window(WoeEdition.Fe, DayOfWeek.Saturday, "20:00", "22:00"));

        s.Tick(Sat(19, 59));
        Assert.False(agit.IsAgitActive);          // before the window

        s.Tick(Sat(20, 0));
        Assert.True(agit.IsAgitActive);           // opened

        s.Tick(Sat(20, 30));                      // mid-window: no re-fire, still active
        Assert.True(agit.IsAgitActive);

        s.Tick(Sat(22, 0));
        Assert.False(agit.IsAgitActive);          // closed at end (exclusive)
    }

    [Fact]
    public void Mid_window_tick_does_not_refire_after_manual_end()
    {
        var (s, agit) = Build(Window(WoeEdition.Fe, DayOfWeek.Saturday, "20:00", "22:00"));
        s.Tick(Sat(20, 0));
        Assert.True(agit.IsAgitActive);

        // A GM manually ends WoE mid-window. The scheduler is edge-triggered: it already fired the
        // open edge, so it must NOT re-start on the next in-window tick.
        agit.AgitEnd();
        Assert.False(agit.IsAgitActive);

        s.Tick(Sat(20, 30));
        Assert.False(agit.IsAgitActive);          // not re-started — manual override respected
    }

    [Fact]
    public void Manual_start_between_windows_is_not_stomped()
    {
        var (s, agit) = Build(Window(WoeEdition.Fe, DayOfWeek.Saturday, "20:00", "22:00"));
        s.Tick(Sat(10, 0));                       // outside any window — establishes "outside" state

        agit.AgitStart();                         // GM starts WoE off-schedule
        Assert.True(agit.IsAgitActive);

        s.Tick(Sat(11, 0));                       // still outside the window
        Assert.True(agit.IsAgitActive);           // scheduler did NOT end it (level-enforcement would have)
    }

    [Fact]
    public void Boot_inside_a_window_starts_immediately()
    {
        var (s, agit) = Build(Window(WoeEdition.Fe, DayOfWeek.Saturday, "20:00", "22:00"));
        s.Tick(Sat(20, 30));                      // first tick is already inside the window
        Assert.True(agit.IsAgitActive);
    }

    [Fact]
    public void Editions_are_independent()
    {
        var (s, agit) = Build(
            Window(WoeEdition.Fe, DayOfWeek.Saturday, "20:00", "22:00"),
            Window(WoeEdition.Se, DayOfWeek.Saturday, "21:00", "23:00"));

        s.Tick(Sat(20, 30));
        Assert.True(agit.IsAgitActive);
        Assert.False(agit.IsAgit2Active);

        s.Tick(Sat(21, 30));
        Assert.True(agit.IsAgitActive);
        Assert.True(agit.IsAgit2Active);

        s.Tick(Sat(22, 30));                      // FE closed, SE still open
        Assert.False(agit.IsAgitActive);
        Assert.True(agit.IsAgit2Active);
    }

    [Fact]
    public void Window_crossing_midnight_is_handled()
    {
        var (s, agit) = Build(Window(WoeEdition.Fe, DayOfWeek.Saturday, "23:00", "01:00"));

        s.Tick(Sat(23, 30));
        Assert.True(agit.IsAgitActive);           // Saturday late-night part

        s.Tick(Sun(0, 30));
        Assert.True(agit.IsAgitActive);           // rolled into Sunday early-morning part

        s.Tick(Sun(1, 0));
        Assert.False(agit.IsAgitActive);          // closed
    }

    [Fact]
    public void Reload_via_config_picks_up_the_schedule()
    {
        var config = new MapServerConfiguration();
        var agit = new AgitService(NullLogger<AgitService>.Instance);
        var sched = new WoeScheduler(agit, NullLogger<WoeScheduler>.Instance, config);
        Assert.Empty(sched.Windows);

        config.WoeSchedule.Add(new WoeScheduleEntry { Type = "1.0", Day = "Saturday", Start = "20:00", End = "22:00" });
        config.WoeSchedule.Add(new WoeScheduleEntry { Type = "bogus", Day = "Saturday", Start = "x", End = "y" });
        sched.Reload();

        Assert.Single(sched.Windows);             // malformed entry skipped
        sched.Tick(Sat(20, 30));
        Assert.True(agit.IsAgitActive);
    }
}
