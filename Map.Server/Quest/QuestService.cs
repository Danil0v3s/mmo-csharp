using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Quest;

/// <summary>
/// Default <see cref="IQuestService"/>. Catalog loaded from the
/// <c>quest_db</c> SQL table (seeded by Tools.RathenaImporter from
/// <c>db/re/quest_db.yml</c>, ~4,800 quests). Per-character active
/// quest log lives on the persistent quest table accessed via IPC;
/// this service owns the *catalog* lookup + the objective-update
/// pipeline.
/// </summary>
public sealed class QuestService : IQuestService
{
    private readonly Dictionary<uint, QuestDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<QuestService> _logger;

    public QuestService(IServiceScopeFactory scopes, ILogger<QuestService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public QuestService(ILogger<QuestService> logger) { _logger = logger; }

    public int Add(PlayerEntity pc, int questId) => 0;
    public int Change(PlayerEntity pc, int oldQuestId, int newQuestId) => 0;
    public int Check(PlayerEntity pc, int questId, byte status) => 0;
    public int Delete(PlayerEntity pc, int questId) => 0;
    public int PcLogin(PlayerEntity pc) => 0;
    public int UpdateObjectiveSub(PlayerEntity pc, int questId, byte index, int delta) => 0;

    /// <summary>
    /// FEATURE-01 — rAthena <c>quest_update_objective</c> single-objective bump: add
    /// <paramref name="delta"/> to objective <paramref name="index"/> of the active quest
    /// <paramref name="questId"/> (capped at the catalog target), completing the quest when every
    /// objective is satisfied.
    /// </summary>
    public void UpdateObjective(PlayerEntity pc, int questId, byte index, int delta)
    {
        if (delta == 0) return;
        var q = FindActive(pc, questId);
        if (q == null) return;
        var cat = GetCatalogEntry((uint)questId);
        if (cat == null) return;
        var target = ObjectiveTarget(cat, index);
        if (target <= 0) return; // no such objective
        EnsureCounts(q, index + 1);
        var updated = Math.Min(target, q.Counts[index] + delta);
        if (updated == q.Counts[index]) return;
        q.Counts[index] = updated;
        TryComplete(q, cat);
    }

    /// <inheritdoc />
    public void UpdateMobObjective(PlayerEntity pc, string mobAegis)
    {
        if (string.IsNullOrEmpty(mobAegis)) return;
        foreach (var q in pc.QuestLog)
        {
            if (q.State != 1) continue; // Q_ACTIVE only (rAthena skips Q_COMPLETE)
            var cat = GetCatalogEntry((uint)q.QuestId);
            if (cat == null) continue;

            var changed = false;
            for (byte i = 0; i < 3; i++)
            {
                if (!ObjectiveMobMatches(cat, i, mobAegis)) continue;
                var target = ObjectiveTarget(cat, i);
                if (target <= 0) continue;
                EnsureCounts(q, i + 1);
                if (q.Counts[i] >= target) continue;
                q.Counts[i]++;
                changed = true;
            }
            if (changed) TryComplete(q, cat);
            // ZC_UPDATE_MISSION_HUNT client emit is owned by PACKET-10 (see QUEST-UI follow-up);
            // the count + state mutated here rides the existing QuestSaveAsync fan-out.
        }
    }

    public int UpdateStatus(PlayerEntity pc, int questId, byte status) => 0;

    // --- FEATURE-01 objective helpers ---

    private static QuestEntry? FindActive(PlayerEntity pc, int questId)
    {
        foreach (var q in pc.QuestLog)
            if (q.QuestId == questId && q.State == 1) return q;
        return null;
    }

    private static bool ObjectiveMobMatches(Core.Database.Entities.QuestDbEntity cat, byte index, string mobAegis)
    {
        var mob = index switch { 0 => cat.Mob1, 1 => cat.Mob2, 2 => cat.Mob3, _ => null };
        return !string.IsNullOrEmpty(mob) && string.Equals(mob, mobAegis, StringComparison.OrdinalIgnoreCase);
    }

    private static int ObjectiveTarget(Core.Database.Entities.QuestDbEntity cat, byte index) => index switch
    {
        0 => string.IsNullOrEmpty(cat.Mob1) ? 0 : cat.Count1,
        1 => string.IsNullOrEmpty(cat.Mob2) ? 0 : cat.Count2,
        2 => string.IsNullOrEmpty(cat.Mob3) ? 0 : cat.Count3,
        _ => 0,
    };

    private static void EnsureCounts(QuestEntry q, int length)
    {
        if (q.Counts.Length >= length) return;
        var grown = new int[length];
        Array.Copy(q.Counts, grown, q.Counts.Length);
        q.Counts = grown;
    }

    /// <summary>Flip the quest to Q_COMPLETE (2) once every mob objective hits its target.</summary>
    private static void TryComplete(QuestEntry q, Core.Database.Entities.QuestDbEntity cat)
    {
        for (byte i = 0; i < 3; i++)
        {
            var target = ObjectiveTarget(cat, i);
            if (target <= 0) continue;
            if (i >= q.Counts.Length || q.Counts[i] < target) return;
        }
        q.State = 2; // Q_COMPLETE
    }

    public void Reload()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IQuestDbRepository>();
            foreach (var q in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[q.QuestId] = q;
            _logger.LogInformation("quest_db loaded: {N} quests", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "quest_db load failed");
        }
    }

    /// <summary>FEATURE-01 test seam — seed catalog rows without a DB round-trip.</summary>
    internal void SeedCatalogForTest(params QuestDbEntity[] entries)
    {
        foreach (var e in entries) _catalog[e.QuestId] = e;
    }

    /// <summary>Catalog lookup — null if unknown id.</summary>
    public QuestDbEntity? GetCatalogEntry(uint questId)
        => _catalog.TryGetValue(questId, out var v) ? v : null;

    /// <inheritdoc />
    public IReadOnlyList<Core.Server.IPC.QuestEntryData> SnapshotFor(PlayerEntity pc)
    {
        // Mirrors rAthena `intif_quest_save` payload shape: one entry
        // per active quest with the per-objective counters + the
        // unix-time + state field.
        var log = pc.QuestLog;
        if (log.Count == 0) return Array.Empty<Core.Server.IPC.QuestEntryData>();
        var snapshot = new Core.Server.IPC.QuestEntryData[log.Count];
        for (int i = 0; i < log.Count; i++)
        {
            var q = log[i];
            var entry = new Core.Server.IPC.QuestEntryData
            {
                QuestId = q.QuestId,
                TimeUnix = q.TimeUnix,
                State = q.State,
            };
            if (q.Counts != null)
                foreach (var c in q.Counts)
                    entry.Counts.Add(c);
            snapshot[i] = entry;
        }
        return snapshot;
    }

    /// <inheritdoc />
    public void Hydrate(PlayerEntity pc, IEnumerable<Core.Server.IPC.QuestEntryData> entries)
    {
        pc.QuestLog.Clear();
        if (entries == null) return;
        foreach (var e in entries)
        {
            pc.QuestLog.Add(new QuestEntry
            {
                QuestId = e.QuestId,
                TimeUnix = e.TimeUnix,
                State = e.State,
                Counts = e.Counts?.ToArray() ?? Array.Empty<int>(),
            });
        }
    }
}
