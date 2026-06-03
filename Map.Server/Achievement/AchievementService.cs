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
    private readonly Map.Server.Status.ISessionManagerAccessor? _sessions;
    private readonly Map.Server.Items.IItemCatalog? _items;
    private readonly Map.Server.Inventory.IInventoryService? _inventory;
    private readonly ILogger<AchievementService> _logger;

    // FEATURE-04 — achievement-level XP curve (rAthena achievement_level_db), lazily loaded.
    private List<AchievementLevelDbEntity>? _levelCurve;

    // FEATURE-01 — parsed mob-keyed objective targets (rAthena ad->targets), built lazily once
    // the mob_db is available so aegis-name targets resolve to class ids. Key = achievement id.
    private readonly Dictionary<uint, ParsedMobAchievement> _mobTargets = new();
    private readonly HashSet<int> _referencedMobIds = new();
    private bool _mobTargetsParsed;

    private sealed record ParsedMobAchievement(AchievementGroup Group, IReadOnlyList<(int MobId, int Target)> Targets);

    public AchievementService(IServiceScopeFactory scopes, ILogger<AchievementService> logger,
        IMobDb? mobDb = null, Map.Server.Status.ISessionManagerAccessor? sessions = null,
        Map.Server.Items.IItemCatalog? items = null, Map.Server.Inventory.IInventoryService? inventory = null)
    {
        _scopes = scopes;
        _mobDb = mobDb;
        _sessions = sessions;
        _items = items;
        _inventory = inventory;
        _logger = logger;
        ReloadDb();
    }

    public AchievementService(ILogger<AchievementService> logger) { _logger = logger; }

    /// <summary>FEATURE-01 test ctor — sets the mob_db for aegis-name target resolution, no DB load.</summary>
    internal AchievementService(ILogger<AchievementService> logger, IMobDb mobDb) { _logger = logger; _mobDb = mobDb; }

    /// <summary>FEATURE-04 test ctor — wires the reward-grant deps without a DB load.</summary>
    internal AchievementService(ILogger<AchievementService> logger, IMobDb? mobDb,
        Map.Server.Status.ISessionManagerAccessor? sessions, Map.Server.Items.IItemCatalog? items,
        Map.Server.Inventory.IInventoryService? inventory)
    { _logger = logger; _mobDb = mobDb; _sessions = sessions; _items = items; _inventory = inventory; }

    /// <summary>FEATURE-04 test seam — seed the achievement-level curve without a DB round-trip.</summary>
    internal void SeedLevelCurveForTest(params AchievementLevelDbEntity[] rows)
        => _levelCurve = new List<AchievementLevelDbEntity>(rows);

    /// <summary>FEATURE-04 — rAthena <c>achievement_check_condition</c>: evaluate the catalog
    /// <c>Condition</c> script. No condition scripts are stored in the current schema (all achievements
    /// are conditionless), so this returns true; storing + evaluating conditions is FEATURE-24.</summary>
    public bool CheckCondition(PlayerEntity pc, int achievementId) => true;

    /// <summary>FEATURE-04 — rAthena <c>achievement_check_dependent</c>: every prerequisite
    /// achievement id in the catalog <c>Dependents</c> list must be completed for the PC.</summary>
    public bool CheckDependent(PlayerEntity pc, int achievementId)
    {
        var cat = GetCatalogEntry((uint)achievementId);
        if (cat == null || string.IsNullOrEmpty(cat.Dependents)) return true; // no prereqs
        foreach (var tok in cat.Dependents.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(tok, out var depId)) continue;
            var dep = FindEntry(pc, depId);
            if (dep == null || dep.CompletedUnix == 0) return false; // prereq not completed
        }
        return true;
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_remove</c>: drop the achievement from the PC log.</summary>
    public bool Remove(PlayerEntity pc, int achievementId)
    {
        for (var i = 0; i < pc.AchievementLog.Count; i++)
        {
            if (pc.AchievementLog[i].AchievementId != achievementId) continue;
            pc.AchievementLog.RemoveAt(i);
            return true;
        }
        return false;
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_update_achievement</c>: mark an achievement
    /// complete (stamp <c>CompletedUnix</c> + score) or clear it. Returns true when applied.</summary>
    public bool UpdateAchievement(PlayerEntity pc, int achievementId, bool completed)
    {
        var cat = GetCatalogEntry((uint)achievementId);
        if (cat == null) return false;
        var entry = GetOrCreateEntry(pc, achievementId, 0);
        if (completed)
        {
            if (entry.CompletedUnix == 0) entry.CompletedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            entry.Score = cat.Score;
        }
        else
        {
            entry.CompletedUnix = 0;
            entry.Score = 0;
        }
        return true;
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_check_progress</c>: the PC's summed objective
    /// progress for the achievement (0 if not started).</summary>
    public int CheckProgress(PlayerEntity pc, int achievementId)
    {
        var entry = FindEntry(pc, achievementId);
        if (entry == null) return 0;
        var sum = 0;
        foreach (var c in entry.Counts) sum += c;
        return sum;
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_update_objective_sub</c>: add <paramref name="delta"/>
    /// to objective <paramref name="objective"/> (capped at its parsed target), returning the new
    /// count, or -1 when the achievement/objective isn't a known mob target.</summary>
    public int UpdateObjectiveSub(PlayerEntity pc, int achievementId, byte objective, int delta)
    {
        EnsureMobTargetsParsed();
        if (!_mobTargets.TryGetValue((uint)achievementId, out var parsed) || objective >= parsed.Targets.Count)
            return -1;
        var entry = GetOrCreateEntry(pc, achievementId, parsed.Targets.Count);
        entry.Counts[objective] = Math.Clamp(entry.Counts[objective] + delta, 0, parsed.Targets[objective].Target);
        return entry.Counts[objective];
    }
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
            // rAthena gates completion on the dependency chain (achievement_check_dependent).
            if (complete && CheckDependent(pc, (int)achId))
            {
                entry.CompletedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (_catalog.TryGetValue(achId, out var cat)) entry.Score = cat.Score;
                CheckReward(pc, (int)achId); // auto-grant the completion reward
            }
            // The ZC_ACH_UPDATE client emit is owned by PACKET-10 (see ACH-UI follow-up); the
            // count + completion state mutated here rides the existing AchievementSaveAsync fan-out.
        }
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_check_reward</c>: if the achievement is completed,
    /// its dependents are satisfied, and it has not yet been rewarded, grant the reward.</summary>
    public void CheckReward(PlayerEntity pc, int achievementId)
    {
        var entry = FindEntry(pc, achievementId);
        if (entry == null || entry.CompletedUnix == 0 || entry.RewardedUnix != 0) return;
        if (!CheckDependent(pc, achievementId)) return;
        GetReward(pc, achievementId);
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_get_reward</c>: grant the completion reward (item +
    /// title) once, stamping <c>RewardedUnix</c>. Idempotent — a second call no-ops. The bonus reward
    /// <c>Script</c> is the scripting epic → FEATURE-23.</summary>
    public void GetReward(PlayerEntity pc, int achievementId)
    {
        var cat = GetCatalogEntry((uint)achievementId);
        var entry = FindEntry(pc, achievementId);
        if (cat == null || entry == null || entry.CompletedUnix == 0 || entry.RewardedUnix != 0) return;

        if (!string.IsNullOrEmpty(cat.RewardItem) && _sessions != null && _items != null && _inventory != null)
        {
            var session = _sessions.GetByEntityId(pc.Id);
            var itemRow = _items.GetByAegisName(cat.RewardItem);
            if (session != null && itemRow != null)
                _inventory.GiveItem(session, itemRow.Id, Math.Max(1, cat.RewardAmount));
        }
        entry.RewardedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // ZC_REQ_ACH_REWARD_ACK client emit owned by PACKET-10. The bonus reward Script → FEATURE-23.
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_get_titles</c>: the title ids the PC has earned
    /// (rewarded achievements that carry a <c>RewardTitleId</c>).</summary>
    public IReadOnlyList<int> GetTitles(PlayerEntity pc)
    {
        List<int>? titles = null;
        foreach (var e in pc.AchievementLog)
        {
            if (e.RewardedUnix == 0) continue;
            var cat = GetCatalogEntry((uint)e.AchievementId);
            if (cat == null || cat.RewardTitleId <= 0) continue;
            (titles ??= new List<int>()).Add(cat.RewardTitleId);
        }
        return (IReadOnlyList<int>?)titles ?? Array.Empty<int>();
    }

    /// <summary>FEATURE-04 — rAthena <c>achievement_free</c>: drop the in-memory achievement log on
    /// logout (the persisted rows are untouched — they were already snapshotted by the save path).</summary>
    public void Free(PlayerEntity pc) => pc.AchievementLog.Clear();

    /// <summary>FEATURE-04 — rAthena <c>achievement_level</c> (achievement.cpp:811): the PC's
    /// achievement level from their total completed score, walked against the level curve.</summary>
    public int Level(PlayerEntity pc)
    {
        var total = 0;
        foreach (var e in pc.AchievementLog) if (e.CompletedUnix != 0) total += e.Score;

        var curve = EnsureLevelCurve();
        if (curve.Count == 0) return 0;
        // rAthena: advance while total_score > the current level's points; cap at max+1 for display.
        var level = 0;
        while (true)
        {
            var row = curve.Find(r => r.Level == level);
            if (row != null && total > row.RequiredPoints)
            {
                var next = curve.Find(r => r.Level == level + 1);
                level++;
                if (next == null) break;
                continue;
            }
            break;
        }
        return level;
    }

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

    private static AchievementEntry? FindEntry(PlayerEntity pc, int achievementId)
    {
        foreach (var e in pc.AchievementLog) if (e.AchievementId == achievementId) return e;
        return null;
    }

    private List<AchievementLevelDbEntity> EnsureLevelCurve()
    {
        if (_levelCurve != null) return _levelCurve;
        _levelCurve = new List<AchievementLevelDbEntity>();
        if (_scopes == null) return _levelCurve;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAchievementLevelDbRepository>();
            _levelCurve.AddRange(repo.GetAllAsync().GetAwaiter().GetResult());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "achievement_level_db load failed");
        }
        return _levelCurve;
    }

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
