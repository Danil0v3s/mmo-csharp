using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Unified read-only cache over the four DB-8d job catalogs:
/// <list type="bullet">
///   <item><c>job_info_db</c> — per-job MaxWeight + HpFactor/HpIncrease + SpFactor/SpIncrease (171 rows).</item>
///   <item><c>job_max_level_db</c> — per-job MaxBaseLevel / MaxJobLevel (171 rows).</item>
///   <item><c>job_base_points_db</c> — per-job per-level HP/SP/AP (~20k rows).</item>
///   <item><c>job_bonus_stats_db</c> — per-job per-level Str/Agi/.../trait bonuses applied at level-up (6477 rows).</item>
/// </list>
/// DBR-1d loads all four at boot (DBR-0 BattlegroundService pattern);
/// thereafter callers hit in-memory dictionaries keyed by job Aegis name.
/// </summary>
public interface IJobStatsCacheService
{
    /// <summary>
    /// rAthena rule: if <c>job_base_points_db</c> has an explicit HP row
    /// for <paramref name="jobAegis"/> at <paramref name="level"/>, use
    /// it; otherwise fall back to the formula
    /// <c>(level − 1) × HpIncrease / 100 + HpFactor × levelFactor</c>
    /// using the JobInfo row's HpFactor/HpIncrease.
    /// Returns the captured Novice baseline (35 + level*5 scaled by Vit
    /// at the call site) when both the catalog row and the JobInfo entry
    /// are missing.
    /// </summary>
    int GetBaseHp(string jobAegis, int level);

    /// <summary>
    /// Same semantics as <see cref="GetBaseHp"/> but for SP — uses
    /// JobInfo.SpFactor + JobInfo.SpIncrease and the job_base_points
    /// row's <c>Sp</c> column.
    /// </summary>
    int GetBaseSp(string jobAegis, int level);

    /// <summary>
    /// Returns the JobInfo row (MaxWeight / HpFactor / HpIncrease / etc.)
    /// for <paramref name="jobAegis"/>, or null when the row isn't seeded
    /// (e.g. 4th-class jobs whose info hasn't been imported yet).
    /// </summary>
    JobInfoDbEntity? GetJobInfo(string jobAegis);

    /// <summary>
    /// rAthena <c>job_db.MaxBaseLevel</c> — per-job base-level cap.
    /// Default 99 (1st/2nd-class) when the row isn't present, matching
    /// rAthena's pre-3rd-class behaviour.
    /// </summary>
    int GetMaxBaseLevel(string jobAegis);

    /// <summary>rAthena <c>job_db.MaxJobLevel</c> — per-job job-level cap. Default 50.</summary>
    int GetMaxJobLevel(string jobAegis);

    /// <summary>
    /// Sum of <c>job_bonus_stats_db</c> rows for the job at every level
    /// <c>1..currentLevel</c>. Used by the status calc to layer
    /// per-level-up stat bumps (Str/Agi/Vit/Int/Dex/Luk + renewal trait
    /// stats) on top of the player's allocated points.
    /// </summary>
    JobBonusStatsSum GetBonusSum(string jobAegis, int currentLevel);
}

/// <summary>
/// Aggregated bonus-stat totals from <see cref="IJobStatsCacheService.GetBonusSum"/>.
/// </summary>
public readonly record struct JobBonusStatsSum(
    int Str, int Agi, int Vit, int Int, int Dex, int Luk,
    int Pow, int Sta, int Wis, int Spl, int Con, int Crt);

/// <summary>
/// Default <see cref="IJobStatsCacheService"/>. Loads all four catalogs
/// once at boot, then serves lookups from in-memory dictionaries.
/// </summary>
public sealed class JobStatsCacheService : IJobStatsCacheService
{
    private readonly Dictionary<string, JobInfoDbEntity> _jobInfo = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int? MaxBase, int? MaxJob)> _maxLevels = new(StringComparer.Ordinal);
    // Job → ordered level → (Hp, Sp, Ap) row.
    private readonly Dictionary<string, Dictionary<int, (int Hp, int Sp, int? Ap)>> _basePoints = new(StringComparer.Ordinal);
    // Job → ordered level → bonus stat row.
    private readonly Dictionary<string, List<JobBonusStatsDbEntity>> _bonusStats = new(StringComparer.Ordinal);
    private readonly ILogger<JobStatsCacheService> _logger;

    public JobStatsCacheService(IServiceScopeFactory scopes, ILogger<JobStatsCacheService> logger)
    {
        _logger = logger;
        try
        {
            using var scope = scopes.CreateScope();
            var info = scope.ServiceProvider.GetRequiredService<IJobInfoDbRepository>();
            var exp = scope.ServiceProvider.GetRequiredService<IJobExpDbRepository>();
            var basePoints = scope.ServiceProvider.GetRequiredService<IJobBasePointsDbRepository>();

            // Stage 1: JobInfo (1 query, 171 rows).
            foreach (var row in info.GetAllAsync().GetAwaiter().GetResult())
            {
                _jobInfo[row.JobAegis] = row;
            }

            // Stage 2: per-job loaders. We've already got every job Aegis
            // name from stage 1; iterating it gives us 171 small queries
            // for max-level / base-points / bonus-stats instead of 3 full
            // table scans + post-grouping. Both work, but the per-job
            // form keeps the repository signatures unchanged.
            foreach (var jobAegis in _jobInfo.Keys)
            {
                var max = exp.GetMaxLevelAsync(jobAegis).GetAwaiter().GetResult();
                if (max != null) _maxLevels[jobAegis] = (max.MaxBaseLevel, max.MaxJobLevel);

                var rows = basePoints.GetByJobAsync(jobAegis).GetAwaiter().GetResult();
                if (rows.Count > 0)
                {
                    var dict = new Dictionary<int, (int, int, int?)>(rows.Count);
                    foreach (var r in rows) dict[r.Level] = (r.Hp, r.Sp, r.Ap);
                    _basePoints[jobAegis] = dict;
                }

                var bonus = info.GetBonusStatsAsync(jobAegis).GetAwaiter().GetResult();
                if (bonus.Count > 0)
                {
                    _bonusStats[jobAegis] = bonus.ToList();
                }
            }

            _logger.LogInformation(
                "Job stats catalogs hydrated from DB: {Jobs} jobs / {BasePoints} HP-SP rows / {Bonus} bonus-stat rows",
                _jobInfo.Count, _basePoints.Values.Sum(d => d.Count), _bonusStats.Values.Sum(l => l.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job stats DB load failed; falling back to rAthena formula defaults");
        }
    }

    public int GetBaseHp(string jobAegis, int level)
    {
        if (level < 1) level = 1;
        // Explicit row wins (rAthena: HP_SP_TABLES macro path).
        if (_basePoints.TryGetValue(jobAegis, out var dict) && dict.TryGetValue(level, out var row))
            return row.Hp;
        // Formula fallback — JobInfo's HpFactor + HpIncrease.
        if (_jobInfo.TryGetValue(jobAegis, out var info))
        {
            // rAthena status.cpp:status_base_hp formula (pre-tables):
            //   hp = (lv-1) * hp_increase / 100 + hp_factor * lv
            // (renewal job_db has no level-1 row; formula derives from lv1).
            return Math.Max(1, (level - 1) * info.HpIncrease / 100 + info.HpFactor * level);
        }
        return 0; // caller folds in the Novice fallback when this returns 0.
    }

    public int GetBaseSp(string jobAegis, int level)
    {
        if (level < 1) level = 1;
        if (_basePoints.TryGetValue(jobAegis, out var dict) && dict.TryGetValue(level, out var row))
            return row.Sp;
        if (_jobInfo.TryGetValue(jobAegis, out var info))
        {
            return Math.Max(1, (level - 1) * info.SpIncrease / 100 + info.SpFactor * level);
        }
        return 0;
    }

    public JobInfoDbEntity? GetJobInfo(string jobAegis)
        => _jobInfo.TryGetValue(jobAegis, out var info) ? info : null;

    public int GetMaxBaseLevel(string jobAegis)
        => _maxLevels.TryGetValue(jobAegis, out var caps) && caps.MaxBase is int v ? v : 99;

    public int GetMaxJobLevel(string jobAegis)
        => _maxLevels.TryGetValue(jobAegis, out var caps) && caps.MaxJob is int v ? v : 50;

    public JobBonusStatsSum GetBonusSum(string jobAegis, int currentLevel)
    {
        if (!_bonusStats.TryGetValue(jobAegis, out var rows))
            return default;

        int str = 0, agi = 0, vit = 0, ist = 0, dex = 0, luk = 0;
        int pow = 0, sta = 0, wis = 0, spl = 0, con = 0, crt = 0;
        foreach (var r in rows)
        {
            if (r.Level > currentLevel) continue;
            str += r.Str; agi += r.Agi; vit += r.Vit;
            ist += r.IntStat; dex += r.Dex; luk += r.Luk;
            pow += r.Pow; sta += r.Sta; wis += r.Wis;
            spl += r.Spl; con += r.Con; crt += r.Crt;
        }
        return new JobBonusStatsSum(str, agi, vit, ist, dex, luk, pow, sta, wis, spl, con, crt);
    }
}
