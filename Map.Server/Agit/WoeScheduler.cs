using Microsoft.Extensions.Logging;

namespace Map.Server.Agit;

/// <summary>WoE edition a schedule window drives.</summary>
public enum WoeEdition
{
    /// <summary>WoE 1.0 (FE) — <see cref="IAgitService.AgitStart"/>.</summary>
    Fe = 1,
    /// <summary>WoE 2.0 (SE) — <see cref="IAgitService.Agit2Start"/>.</summary>
    Se = 2,
    /// <summary>WoE TE — <see cref="IAgitService.Agit3Start"/>.</summary>
    Te = 3,
}

/// <summary>
/// One recurring WoE window: <paramref name="Edition"/> runs from <paramref name="Start"/> on
/// <paramref name="Day"/> until <paramref name="End"/>. When <c>End ≤ Start</c> the window crosses
/// midnight into the following day. Times are server-local.
/// </summary>
public readonly record struct WoeWindow(WoeEdition Edition, DayOfWeek Day, TimeOnly Start, TimeOnly End)
{
    /// <summary>True iff <paramref name="now"/> (server-local) falls inside this window.</summary>
    public bool Contains(DateTime now)
    {
        if (Start == End) return false; // zero-length window = never active
        var t = TimeOnly.FromDateTime(now);
        if (Start < End)
            return now.DayOfWeek == Day && t >= Start && t < End;
        // Crosses midnight: [Day Start, 24:00) ∪ [next day 00:00, End).
        var openPart = now.DayOfWeek == Day && t >= Start;
        var closePart = now.DayOfWeek == NextDay(Day) && t < End;
        return openPart || closePart;
    }

    private static DayOfWeek NextDay(DayOfWeek d) => (DayOfWeek)(((int)d + 1) % 7);

    /// <summary>Parse a config entry; returns false (and logs nothing) on a malformed row.</summary>
    public static bool TryParse(WoeScheduleEntry e, out WoeWindow window)
    {
        window = default;
        var edition = e.Type?.Trim().ToUpperInvariant() switch
        {
            "1.0" or "FE" or "1" => WoeEdition.Fe,
            "2.0" or "SE" or "2" => WoeEdition.Se,
            "TE" or "3.0" or "3" => WoeEdition.Te,
            _ => (WoeEdition?)null,
        };
        if (edition == null) return false;
        if (!Enum.TryParse<DayOfWeek>(e.Day?.Trim(), ignoreCase: true, out var day)) return false;
        if (!TimeOnly.TryParse(e.Start?.Trim(), out var start)) return false;
        if (!TimeOnly.TryParse(e.End?.Trim(), out var end)) return false;
        window = new WoeWindow(edition.Value, day, start, end);
        return true;
    }
}

/// <summary>
/// Drives <see cref="IAgitService"/> from a weekly schedule. rAthena runs WoE off
/// <c>OnClock&lt;HHMM&gt;</c> NPC-script labels in <c>npc/guild/agit_controller.txt</c> that call
/// <c>AgitStart</c>/<c>AgitEnd</c> at the window edges; this C# port keeps the schedule in config and
/// fires the same edge transitions. Behaviour matches: WoE auto-starts at window-open and auto-ends
/// at window-close. (See FEATURE-15.)
/// </summary>
public interface IWoeScheduler
{
    /// <summary>The active schedule (server-local).</summary>
    IReadOnlyList<WoeWindow> Windows { get; }

    /// <summary>Evaluate the schedule at <paramref name="nowLocal"/> and fire the WoE edge transitions
    /// (start at window-open, end at window-close). Called on a coarse cadence from the game loop.</summary>
    void Tick(DateTime nowLocal);

    /// <summary>Re-read the schedule from config (GM <c>@reloadscript</c> equivalent).</summary>
    void Reload();
}

/// <summary>
/// Default <see cref="IWoeScheduler"/>. **Edge-triggered**: fires <c>AgitStart</c> exactly when a
/// window opens and <c>AgitEnd</c> exactly when it closes — it does NOT enforce "off" every tick, so a
/// GM/script manual <c>@agitstart</c> between windows is not stomped. A server booting *inside* a
/// window fires the start on the first tick (previous-state initialises to "outside").
/// </summary>
public sealed class WoeScheduler : IWoeScheduler
{
    private readonly IAgitService _agit;
    private readonly MapServerConfiguration? _config;
    private readonly ILogger<WoeScheduler> _logger;

    private List<WoeWindow> _windows = new();
    // Per-edition "were we inside a window at the last evaluation?" — the edge detector.
    private readonly Dictionary<WoeEdition, bool> _wasInside = new()
    {
        [WoeEdition.Fe] = false, [WoeEdition.Se] = false, [WoeEdition.Te] = false,
    };

    public WoeScheduler(IAgitService agit, ILogger<WoeScheduler> logger, MapServerConfiguration? config = null)
    {
        _agit = agit;
        _logger = logger;
        _config = config;
        Reload();
    }

    public IReadOnlyList<WoeWindow> Windows => _windows;

    public void Reload()
    {
        var parsed = new List<WoeWindow>();
        if (_config != null)
        {
            foreach (var e in _config.WoeSchedule)
            {
                if (WoeWindow.TryParse(e, out var w)) parsed.Add(w);
                else _logger.LogWarning("WoE schedule: skipped malformed entry Type={Type} Day={Day} {Start}-{End}",
                    e.Type, e.Day, e.Start, e.End);
            }
        }
        _windows = parsed;
        _logger.LogInformation("WoE schedule loaded: {N} window(s)", _windows.Count);
    }

    /// <summary>Test seam — set the schedule without config.</summary>
    public void SetWindows(IEnumerable<WoeWindow> windows) => _windows = windows.ToList();

    public void Tick(DateTime nowLocal)
    {
        if (_windows.Count == 0) return;
        foreach (var edition in new[] { WoeEdition.Fe, WoeEdition.Se, WoeEdition.Te })
        {
            var inside = false;
            foreach (var w in _windows)
                if (w.Edition == edition && w.Contains(nowLocal)) { inside = true; break; }

            var was = _wasInside[edition];
            if (inside && !was) Start(edition);       // leading edge → open WoE
            else if (!inside && was) End(edition);    // trailing edge → close WoE
            _wasInside[edition] = inside;
        }
    }

    private void Start(WoeEdition ed)
    {
        var fired = ed switch
        {
            WoeEdition.Fe => _agit.AgitStart(),
            WoeEdition.Se => _agit.Agit2Start(),
            WoeEdition.Te => _agit.Agit3Start(),
            _ => false,
        };
        if (fired) _logger.LogInformation("WoE scheduler: {Edition} window opened → started", ed);
    }

    private void End(WoeEdition ed)
    {
        var fired = ed switch
        {
            WoeEdition.Fe => _agit.AgitEnd(),
            WoeEdition.Se => _agit.Agit2End(),
            WoeEdition.Te => _agit.Agit3End(),
            _ => false,
        };
        if (fired) _logger.LogInformation("WoE scheduler: {Edition} window closed → ended", ed);
    }
}
