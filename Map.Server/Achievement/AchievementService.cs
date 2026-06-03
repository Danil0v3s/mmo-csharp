using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Map.Server.Mob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Achievement;

/// <summary>
/// Default <see cref="IAchievementService"/>. Catalog loaded from
/// <c>achievement_db</c> (seeded from
/// <c>db/re/achievement_db.yml</c>, ~362 rows). Per-character
/// progress lives on the achievement table accessed via IPC.
/// </summary>
public sealed class AchievementService : IAchievementService
{
    private readonly Dictionary<uint, AchievementDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly IMobDb? _mobDb;
    private readonly ILogger<AchievementService> _logger;

    // FEATURE-01 — parsed mob-keyed objective targets (rAthena ad->targets), built lazily once
    // the mob_db is available so aegis-name targets resolve to class ids. Key = achievement id.
    private readonly Dictionary<uint, ParsedMobAchievement> _mobTargets = new();
    private readonly HashSet<int> _referencedMobIds = new();
    private bool _mobTargetsParsed;

    private sealed record ParsedMobAchievement(AchievementGroup Group, IReadOnlyList<(int MobId, int Target)> Targets);

    public AchievementService(IServiceScopeFactory scopes, ILogger<AchievementService> logger, IMobDb? mobDb = null)
    {
        _scopes = scopes;
        _mobDb = mobDb;
        _logger = logger;
        ReloadDb();
    }

    public AchievementService(ILogger<AchievementService> logger) { _logger = logger; }

    /// <summary>FEATURE-01 test ctor — sets the mob_db for aegis-name target resolution, no DB load.</summary>
    internal AchievementService(ILogger<AchievementService> logger, IMobDb mobDb) { _logger = logger; _mobDb = mobDb; }

    public bool CheckCondition(PlayerEntity pc, int achievementId) => false;
    public bool CheckDependent(PlayerEntity pc, int achievementId) => false;
    public bool Remove(PlayerEntity pc, int achievementId) => false;
    public bool UpdateAchievement(PlayerEntity pc, int achievementId, bool completed) => false;
    public int CheckProgress(PlayerEntity pc, int achievementId) => 0;
    public int UpdateObjectiveSub(PlayerEntity pc, int achievementId, byte objective, int delta) => 0;
    /// <summary>
    /// FEATURE-01 — rAthena <c>achievement_update_objective</c> (achievement.cpp:930) for the
    /// mob-keyed groups (AG_BATTLE / AG_TAMING). <paramref name="type"/> is the
    /// <see cref="AchievementGroup"/>, <paramref name="value"/> is the killed mob's class id; every
    /// catalog achievement of that group whose target list references the mob gets its matching
    /// objective counter bumped (capped at the target), and the achievement is marked complete the
    /// moment every objective is satisfied. <paramref name="index"/> is unused for mob groups
    /// (rAthena keys off the mob id, not a fixed objective slot).
    /// </summary>
    public void UpdateObjective(PlayerEntity pc, byte type, byte index, int value)
    {
        var keyword = GroupKeyword((AchievementGroup)type);
        if (keyword == null) return; // non-mob group — not driven from the kill path
        EnsureMobTargetsParsed();

        foreach (var (achId, parsed) in _mobTargets)
        {
            if (GroupKeyword(parsed.Group) != keyword) continue;
            // Does this achievement reference the killed mob at all?
            var refsMob = false;
            for (var i = 0; i < parsed.Targets.Count; i++)
                if (parsed.Targets[i].MobId == value) { refsMob = true; break; }
            if (!refsMob) continue;

            var entry = GetOrCreateEntry(pc, (int)achId, parsed.Targets.Count);
            if (entry.CompletedUnix != 0) continue; // already done — rAthena skips

            var changed = false;
            for (var i = 0; i < parsed.Targets.Count; i++)
            {
                if (parsed.Targets[i].MobId != value) continue;
                if (entry.Counts[i] >= parsed.Targets[i].Target) continue;
                entry.Counts[i]++;
                changed = true;
            }
            if (!changed) continue;

            // Completion: every objective counter at or above its target (rAthena
            // achievement_target_complete).
            var complete = true;
            for (var i = 0; i < parsed.Targets.Count; i++)
                if (entry.Counts[i] < parsed.Targets[i].Target) { complete = false; break; }
            if (complete)
            {
                entry.CompletedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (_catalog.TryGetValue(achId, out var cat)) entry.Score = cat.Score;
            }
            // The ZC_ACH_UPDATE client emit is owned by PACKET-10 (see ACH-UI follow-up); the
            // count + completion state mutated here rides the existing AchievementSaveAsync fan-out.
        }
    }

    public void CheckReward(PlayerEntity pc, int achievementId) { }
    public void GetReward(PlayerEntity pc, int achievementId) { }
    public IReadOnlyList<int> GetTitles(PlayerEntity pc) => Array.Empty<int>();
    public void Free(PlayerEntity pc) { }
    public int Level(PlayerEntity pc) => 0;

    /// <summary>
    /// FEATURE-01 — rAthena <c>AchievementDatabase::mobexists</c>: true when any AG_BATTLE / AG_TAMING
    /// achievement references <paramref name="mobId"/> as a kill target. Lets the death observer skip
    /// the per-contributor objective scan for mobs no achievement cares about.
    /// </summary>
    public bool MobExists(int mobId)
    {
        EnsureMobTargetsParsed();
        return _referencedMobIds.Contains(mobId);
    }

    private static string? GroupKeyword(AchievementGroup group) => group switch
    {
        AchievementGroup.Battle => "AG_BATTLE",
        AchievementGroup.Taming => "AG_TAMING",
        _ => null,
    };

    private AchievementEntry GetOrCreateEntry(PlayerEntity pc, int achievementId, int objectiveCount)
    {
        foreach (var e in pc.AchievementLog)
        {
            if (e.AchievementId != achievementId) continue;
            if (e.Counts.Length < objectiveCount)
            {
                var grown = new int[objectiveCount];
                Array.Copy(e.Counts, grown, e.Counts.Length);
                e.Counts = grown;
            }
            return e;
        }
        var entry = new AchievementEntry { AchievementId = achievementId, Counts = new int[objectiveCount] };
        pc.AchievementLog.Add(entry);
        return entry;
    }

    /// <summary>
    /// Parse every AG_BATTLE / AG_TAMING catalog row's <c>Targets</c> string
    /// ("Poring:5;@id=1002:10") into resolved (classId, targetCount) pairs. Aegis-name tokens
    /// resolve through the mob_db; <c>@id=N</c> tokens are taken verbatim. Built once, lazily, so the
    /// mob_db has finished loading.
    /// </summary>
    private void EnsureMobTargetsParsed()
    {
        if (_mobTargetsParsed) return;
        _mobTargetsParsed = true;
        foreach (var (achId, cat) in _catalog)
        {
            var group = ParseGroup(cat.GroupName);
            if (GroupKeyword(group) == null) continue;
            if (string.IsNullOrWhiteSpace(cat.Targets)) continue;

            var targets = new List<(int MobId, int Target)>();
            foreach (var raw in cat.Targets.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var sep = raw.LastIndexOf(':');
                if (sep <= 0) continue;
                var token = raw[..sep].Trim();
                if (!int.TryParse(raw[(sep + 1)..].Trim(), out var count) || count <= 0) continue;

                int mobId;
                if (token.StartsWith("@id=", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(token[4..], out mobId)) continue;
                }
                else
                {
                    var row = _mobDb?.GetByAegisName(token);
                    if (row == null) continue; // unknown mob name — cannot resolve, skip target
                    mobId = row.Id;
                }
                targets.Add((mobId, count));
                _referencedMobIds.Add(mobId);
            }
            if (targets.Count > 0)
                _mobTargets[achId] = new ParsedMobAchievement(group, targets);
        }
    }

    private static AchievementGroup ParseGroup(string? groupName) => groupName?.ToUpperInvariant() switch
    {
        "AG_BATTLE" => AchievementGroup.Battle,
        "AG_TAMING" => AchievementGroup.Taming,
        _ => AchievementGroup.None,
    };

    public void ReloadDb()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAchievementDbRepository>();
            foreach (var a in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[a.AchievementId] = a;
            _logger.LogInformation("achievement_db loaded: {N} achievements", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "achievement_db load failed");
        }
    }

    /// <summary>FEATURE-01 test seam — seed catalog rows + reset the lazy target parse.</summary>
    internal void SeedCatalogForTest(params AchievementDbEntity[] entries)
    {
        foreach (var e in entries) _catalog[e.AchievementId] = e;
        _mobTargetsParsed = false;
        _mobTargets.Clear();
        _referencedMobIds.Clear();
    }

    /// <summary>Catalog lookup — null if unknown.</summary>
    public AchievementDbEntity? GetCatalogEntry(uint achievementId)
        => _catalog.TryGetValue(achievementId, out var v) ? v : null;

    /// <inheritdoc />
    public IReadOnlyList<Core.Server.IPC.AchievementEntryData> SnapshotFor(PlayerEntity pc)
    {
        var log = pc.AchievementLog;
        if (log.Count == 0) return Array.Empty<Core.Server.IPC.AchievementEntryData>();
        var snapshot = new Core.Server.IPC.AchievementEntryData[log.Count];
        for (int i = 0; i < log.Count; i++)
        {
            var a = log[i];
            var entry = new Core.Server.IPC.AchievementEntryData
            {
                AchievementId = a.AchievementId,
                CompletedUnix = a.CompletedUnix,
                RewardedUnix = a.RewardedUnix,
                Score = a.Score,
            };
            if (a.Counts != null)
                foreach (var c in a.Counts)
                    entry.Counts.Add(c);
            snapshot[i] = entry;
        }
        return snapshot;
    }

    /// <inheritdoc />
    public void Hydrate(PlayerEntity pc, IEnumerable<Core.Server.IPC.AchievementEntryData> entries)
    {
        pc.AchievementLog.Clear();
        if (entries == null) return;
        foreach (var e in entries)
        {
            pc.AchievementLog.Add(new AchievementEntry
            {
                AchievementId = e.AchievementId,
                CompletedUnix = e.CompletedUnix,
                RewardedUnix = e.RewardedUnix,
                Score = e.Score,
                Counts = e.Counts?.ToArray() ?? Array.Empty<int>(),
            });
        }
    }
}
