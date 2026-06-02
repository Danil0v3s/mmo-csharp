using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Per-job per-weapon-type base ASPD delay (rAthena <c>job_aspd.yml</c>,
/// 1427 rows). Used by <c>status_calc_aspd</c> as the starting amotion
/// before AGI/DEX modifiers are subtracted.
///
/// DBR-1c hydrates from <see cref="IJobAspdDbRepository"/> once at boot
/// (DBR-0 BattlegroundService pattern); thereafter every PC stat recalc
/// goes through an O(1) dictionary lookup keyed by (jobAegis, weaponType).
/// Returns the rAthena fallback default 200ms when the (job, weapon) tuple
/// isn't represented in the catalog — matches what rAthena does when a
/// job doesn't ship a row for an exotic weapon class.
/// </summary>
public interface IJobAspdCacheService
{
    /// <summary>
    /// Look up the base ASPD delay (ms) for a (jobAegis, weaponType)
    /// tuple. Falls back to 2000 (rAthena's hardcoded "no row" default,
    /// status.cpp <c>status_get_aspd</c>) when the lookup misses.
    /// </summary>
    int GetBaseAspd(string jobAegis, int weaponType);

    /// <summary>
    /// Look up by numeric job id — convenience wrapper around the Aegis-string
    /// form. Returns the fallback when the job id has no Aegis mapping.
    /// </summary>
    int GetBaseAspdByJobId(int jobId, int weaponType);

    /// <summary>
    /// COMBAT-29 — EXACT (job, weaponType) row, or 0 when not seeded (no
    /// fist/default fallback). Used for the additive shield (weaponType 99) and
    /// dual-wield (<c>aspd_base[wt2]/4</c>) ASPD base terms — a miss must add
    /// nothing, not the "no row" default.
    /// </summary>
    int GetBaseAspdExactByJobId(int jobId, int weaponType);
}

/// <summary>
/// Default <see cref="IJobAspdCacheService"/>. Loads the full job_aspd_db
/// at construction and caches it in a dictionary keyed by composite
/// (jobAegis, weaponType).
/// </summary>
public sealed class JobAspdCacheService : IJobAspdCacheService
{
    /// <summary>rAthena <c>status.cpp status_get_aspd</c> fallback when (job, weapon) is missing.</summary>
    public const int DefaultBaseAspdMs = 2000;

    private readonly Dictionary<(string JobAegis, int WeaponType), int> _table
        = new(EqualityComparer<(string, int)>.Default);
    private readonly ILogger<JobAspdCacheService> _logger;

    public JobAspdCacheService(IServiceScopeFactory scopes, ILogger<JobAspdCacheService> logger)
    {
        _logger = logger;
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IJobAspdDbRepository>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            foreach (var r in rows)
            {
                _table[(r.JobAegis, r.WeaponType)] = r.BaseDelayMs;
            }
            _logger.LogInformation(
                "Job ASPD table hydrated from DB: {Count} (job, weapon) rows",
                _table.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job ASPD DB load failed; all lookups will return the {Fallback}ms fallback", DefaultBaseAspdMs);
        }
    }

    public int GetBaseAspd(string jobAegis, int weaponType)
    {
        if (string.IsNullOrEmpty(jobAegis)) return DefaultBaseAspdMs;
        // Case-sensitive lookup: rAthena yml capitalisation is canonical
        // (e.g. "Knight" / "Rune_Knight"). The seed table preserves it.
        if (_table.TryGetValue((jobAegis, weaponType), out var ms)) return ms;
        // Fall back to bare-hand row for the same job — many jobs only
        // ship weaponType=0 explicitly and rely on the implicit "use my
        // unarmed delay" rule for unlisted weapons.
        if (weaponType != 0 && _table.TryGetValue((jobAegis, 0), out var fist)) return fist;
        return DefaultBaseAspdMs;
    }

    public int GetBaseAspdByJobId(int jobId, int weaponType)
    {
        var aegis = JobAegisMapper.AegisByJobId(jobId);
        return aegis == null ? DefaultBaseAspdMs : GetBaseAspd(aegis, weaponType);
    }

    public int GetBaseAspdExactByJobId(int jobId, int weaponType)
    {
        var aegis = JobAegisMapper.AegisByJobId(jobId);
        if (aegis == null) return 0;
        return _table.TryGetValue((aegis, weaponType), out var ms) ? ms : 0;
    }
}

/// <summary>
/// Numeric JobId → rAthena yml Aegis-name resolver. Mirrors the subset
/// of <c>job_name(class)</c> (pc.cpp:4925) the C# port currently exercises;
/// returns null for unmapped ids (the caller should fall back to the
/// neutral default rather than guessing).
///
/// Only first-class jobs are mapped here — Trans / Baby / 3rd / 4th
/// classes land as their owning subsystems port. The reference for
/// authoritative ids is <c>rathena/src/map/pc.cpp pc_class2idx</c>.
/// </summary>
public static class JobAegisMapper
{
    private static readonly Dictionary<int, string> _byId = new()
    {
        // Class 1 — base classes (rAthena enum JOB_NOVICE..JOB_THIEF)
        { 0, "Novice" },
        { 1, "Swordman" },
        { 2, "Magician" }, // rAthena yml uses "Magician"; "Mage" is the in-game display name
        { 3, "Archer" },
        { 4, "Acolyte" },
        { 5, "Merchant" },
        { 6, "Thief" },
        // Class 2-1 — first-classup
        { 7, "Knight" },
        { 8, "Priest" },
        { 9, "Wizard" },
        { 10, "Blacksmith" },
        { 11, "Hunter" },
        { 12, "Assassin" },
        // Class 2-2
        { 14, "Crusader" },
        { 15, "Monk" },
        { 16, "Sage" },
        { 17, "Rogue" },
        { 18, "Alchemist" },
        { 19, "Bard" },
        { 20, "Dancer" },
        // Extended
        { 23, "Super_Novice" },
        { 24, "Gunslinger" },
        { 25, "Ninja" },
        // Transcendent classes (Trans-only ones not commonly mapped to
        // job_aspd_db share their non-trans Aegis; the cache will fall
        // back if needed). Filled here for completeness:
        { 4002, "Novice_High" },
        { 4003, "Swordman_High" },
        { 4004, "Magician_High" },
        { 4005, "Archer_High" },
        { 4006, "Acolyte_High" },
        { 4007, "Merchant_High" },
        { 4008, "Thief_High" },
    };

    /// <summary>Returns the rAthena yml Aegis name for a JobId, or null when unmapped.</summary>
    public static string? AegisByJobId(int jobId)
        => _byId.TryGetValue(jobId, out var name) ? name : null;
}
