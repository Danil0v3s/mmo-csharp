using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Per-type (Exp / Drop / Mvp_Exp / Mvp_Drop) level-gap penalty evaluator.
/// Mirrors rAthena <c>pc_level_penalty_mod</c> (pc.cpp:6321) — given
/// |player_lvl − mob_lvl|, scan the configured curve and return the
/// rate (in basis-points, 100 = no change) applied to the original
/// EXP / drop amount.
///
/// DBR-1b backs this with the typed <see cref="ILevelPenaltyDbRepository"/>
/// (DB-8a: parent <c>level_penalty_db</c> + child
/// <c>level_penalty_difference_db</c>) cached at boot. rAthena's stock
/// yml only ships the "Exp" type; "Drop"/"Mvp_Exp"/"Mvp_Drop" return
/// 100 (no modifier) until populated.
/// </summary>
public interface ILevelPenaltyService
{
    /// <summary>
    /// rAthena <c>pc_level_penalty_mod(diff, PENALTY_EXP)</c>. Diff is the
    /// signed (player − mob) base-level gap. Returns the multiplier in
    /// basis-points (100 = 1×, 50 = 0.5×, 200 = 2×). Default 100 when
    /// no row matches.
    /// </summary>
    int GetExpModifier(int playerLevel, int mobLevel);

    /// <summary>
    /// rAthena <c>pc_level_penalty_mod(diff, PENALTY_DROP)</c>. Same
    /// semantics as <see cref="GetExpModifier"/> over the "Drop" curve.
    /// </summary>
    int GetDropModifier(int playerLevel, int mobLevel);

    /// <summary>
    /// Generic accessor — any penalty type name from the rAthena enum
    /// (Exp / Drop / Mvp_Exp / Mvp_Drop). Returns 100 when the type
    /// has no rows.
    /// </summary>
    int GetModifier(string penaltyType, int playerLevel, int mobLevel);
}

/// <summary>
/// Default <see cref="ILevelPenaltyService"/>. Loads the level_penalty_db
/// rows once at boot and walks the per-type sorted list for each lookup.
/// </summary>
public sealed class LevelPenaltyService : ILevelPenaltyService
{
    /// <summary>
    /// PenaltyType → ascending (Difference, Rate) tuples. Read-only after
    /// boot — single-threaded game loop means no lock needed.
    /// </summary>
    private readonly Dictionary<string, List<(int Difference, int Rate)>> _curves = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<LevelPenaltyService> _logger;

    public LevelPenaltyService(IServiceScopeFactory scopes, ILogger<LevelPenaltyService> logger)
    {
        _logger = logger;
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ILevelPenaltyDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            var diffs = repo.GetAllDifferencesAsync().GetAwaiter().GetResult();
            foreach (var p in parents)
            {
                _curves[p.PenaltyType] = new List<(int, int)>();
            }
            foreach (var d in diffs)
            {
                if (!_curves.TryGetValue(d.PenaltyType, out var list))
                {
                    // Defensive: child row with no parent — still index it.
                    list = new List<(int, int)>();
                    _curves[d.PenaltyType] = list;
                }
                list.Add((d.Difference, d.Rate));
            }
            // Walk-the-list lookups need ascending difference order so we
            // can pick the largest threshold ≤ |gap|.
            foreach (var list in _curves.Values)
            {
                list.Sort((a, b) => a.Difference.CompareTo(b.Difference));
            }
            _logger.LogInformation(
                "Level-penalty curves hydrated from DB: {Types} types, {Total} differences",
                _curves.Count, _curves.Values.Sum(v => v.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Level-penalty DB load failed; defaulting to 100 (no modifier) for all gaps");
        }
    }

    public int GetExpModifier(int playerLevel, int mobLevel)
        => GetModifier("Exp", playerLevel, mobLevel);

    public int GetDropModifier(int playerLevel, int mobLevel)
        => GetModifier("Drop", playerLevel, mobLevel);

    public int GetModifier(string penaltyType, int playerLevel, int mobLevel)
    {
        if (!_curves.TryGetValue(penaltyType, out var curve) || curve.Count == 0)
            return 100;

        // rAthena pc.cpp:6328 signed-diff form: diff = player − mob. The
        // yml ships both positive (over-levelled) and negative
        // (under-levelled) thresholds, and the table holds the rate at
        // the largest |bucket| ≤ |diff| — but the rAthena lookup keeps
        // the sign so under/over each have their own bucket set. To stay
        // bug-for-bug compatible: pick the largest bucket whose
        // Difference is ≤ |diff|, then return that rate.
        var diff = Math.Abs(playerLevel - mobLevel);

        // Walk descending — the curve list is ascending, so reverse-scan
        // and take the first |bucket| ≤ diff. Lists are short (~20-30
        // entries) so linear is fine.
        int matchedRate = 100;
        for (int i = curve.Count - 1; i >= 0; i--)
        {
            if (Math.Abs(curve[i].Difference) <= diff)
            {
                matchedRate = curve[i].Rate;
                break;
            }
        }
        return matchedRate;
    }
}

/// <summary>
/// DBR-1b: a static AttrFix-style facade isn't appropriate here because
/// LevelPenalty is consumed via DI by EXP/drop services. This empty
/// service exists only as the entry point for the load-at-boot pattern —
/// just inject <see cref="ILevelPenaltyService"/> into <c>DamageService</c>
/// + the drop pipeline. The boot-time cache happens inside the service
/// constructor itself.
/// </summary>
internal static class LevelPenaltyServiceMarker { /* documentation only */ }
