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
    private readonly Map.Server.Status.ISessionManagerAccessor? _sessions;
    private readonly Map.Server.Mob.IMobDb? _mobDb;
    private readonly ILogger<QuestService> _logger;

    // rAthena e_quest_state.
    private const int QInactive = 0, QActive = 1, QComplete = 2;

    /// <summary>FEATURE-03 — server wall clock (rAthena uses <c>localtime</c>/<c>time(nullptr)</c>);
    /// injectable so Add time-limits + PLAYTIME expiry are deterministic in tests.</summary>
    internal Func<DateTimeOffset> Clock { get; set; } = () => DateTimeOffset.Now;

    public QuestService(IServiceScopeFactory scopes, ILogger<QuestService> logger,
        Map.Server.Status.ISessionManagerAccessor? sessions = null, Map.Server.Mob.IMobDb? mobDb = null)
    {
        _scopes = scopes;
        _logger = logger;
        _sessions = sessions;
        _mobDb = mobDb;
        Reload();
    }

    public QuestService(ILogger<QuestService> logger) { _logger = logger; }

    /// <summary>Test seam — inject a session accessor (+ optional mob DB) for the ZC-emit paths.</summary>
    internal QuestService(ILogger<QuestService> logger, Map.Server.Status.ISessionManagerAccessor sessions,
        Map.Server.Mob.IMobDb? mobDb = null)
    { _logger = logger; _sessions = sessions; _mobDb = mobDb; }

    /// <summary>
    /// FEATURE-03 — rAthena <c>quest_add</c> (quest.cpp:587): accept a quest. Fails (-1) on unknown
    /// catalog id or if the PC already has it; else appends a <c>Q_ACTIVE</c> entry with a computed
    /// time limit and zeroed per-objective counters. Returns 0 on success.
    /// </summary>
    public int Add(PlayerEntity pc, int questId)
    {
        var cat = GetCatalogEntry((uint)questId);
        if (cat == null)
        {
            _logger.LogWarning("quest_add: quest {Quest} not in DB", questId);
            return -1;
        }
        if (Check(pc, questId, (byte)QuestCheckType.HaveQuest) >= 0)
            return -1; // rAthena: already has the quest

        var now = Clock();
        pc.QuestLog.Add(new QuestEntry
        {
            QuestId = questId,
            State = QActive,
            Counts = new int[ObjectiveCount(cat)],
            TimeUnix = QuestTime.ParseTimeUnix(cat.TimeLimit, now.ToUnixTimeSeconds(), now),
        });
        EmitAdd(pc, questId); // the log entry persists via the FEATURE-02 quest-save fan-out
        return 0;
    }

    /// <summary>FEATURE-03 — rAthena <c>quest_change</c> (quest.cpp:633): replace
    /// <paramref name="oldQuestId"/> with <paramref name="newQuestId"/> in the same slot. Fails (-1)
    /// if the PC already has the new quest, lacks the old one, or the new id is unknown.</summary>
    public int Change(PlayerEntity pc, int oldQuestId, int newQuestId)
    {
        var newCat = GetCatalogEntry((uint)newQuestId);
        if (newCat == null) return -1;
        if (Check(pc, newQuestId, (byte)QuestCheckType.HaveQuest) >= 0) return -1; // already has new
        var idx = IndexOf(pc, oldQuestId);
        if (idx < 0) return -1; // doesn't have old

        var now = Clock();
        pc.QuestLog[idx] = new QuestEntry
        {
            QuestId = newQuestId,
            State = QActive,
            Counts = new int[ObjectiveCount(newCat)],
            TimeUnix = QuestTime.ParseTimeUnix(newCat.TimeLimit, now.ToUnixTimeSeconds(), now),
        };
        EmitDelete(pc, oldQuestId);
        EmitAdd(pc, newQuestId);
        return 0;
    }

    /// <summary>
    /// FEATURE-03 — rAthena <c>quest_check</c> (quest.cpp:898). Returns -1 if the PC lacks the quest;
    /// otherwise per <see cref="QuestCheckType"/>: HAVEQUEST → the state (Q_INACTIVE reported as
    /// active=1); PLAYTIME → 2 expired / 1 complete / 0 otherwise; HUNTING → 2 objectives met /
    /// 1 expired / 0 otherwise.
    /// </summary>
    public int Check(PlayerEntity pc, int questId, byte status)
    {
        var idx = IndexOf(pc, questId);
        if (idx < 0) return -1;
        var q = pc.QuestLog[idx];
        var nowUnix = Clock().ToUnixTimeSeconds();

        switch ((QuestCheckType)status)
        {
            case QuestCheckType.HaveQuest:
                return q.State == QInactive ? QActive : q.State;
            case QuestCheckType.PlayTime:
                return (q.TimeUnix > 0 && q.TimeUnix < nowUnix) ? 2 : q.State == QComplete ? 1 : 0;
            case QuestCheckType.Hunting:
                if (q.State == QInactive || q.State == QActive)
                {
                    if (AllObjectivesMet(q, GetCatalogEntry((uint)questId))) return 2;
                    if (q.TimeUnix > 0 && q.TimeUnix < nowUnix) return 1;
                }
                return 0;
            default:
                return -1;
        }
    }

    /// <summary>FEATURE-03 — rAthena <c>quest_delete</c> (quest.cpp:684): remove the quest from the
    /// log. Returns 0 if removed, -1 if the PC didn't have it.</summary>
    public int Delete(PlayerEntity pc, int questId)
    {
        var idx = IndexOf(pc, questId);
        if (idx < 0) return -1;
        pc.QuestLog.RemoveAt(idx);
        EmitDelete(pc, questId); // removal persists via the quest-save fan-out
        return 0;
    }

    /// <summary>FEATURE-03 — rAthena <c>quest_pc_login</c> (quest.cpp:560): on session enter (after
    /// <c>Hydrate</c>) push the quest list to the client. No expiry timer is armed — rAthena reports
    /// time-limit expiry on query (<c>PLAYTIME</c>/<c>HUNTING</c>), not via an active state flip.
    /// Returns the active-quest count.</summary>
    public int PcLogin(PlayerEntity pc)
    {
        var active = 0;
        foreach (var q in pc.QuestLog) if (q.State != QComplete) active++;
        EmitList(pc);
        return active;
    }

    /// <summary>rAthena <c>clif_quest_send_list</c> — push the whole quest log on enter so the client
    /// renders the quest window populated. Self-contained: each objective carries live count + target
    /// + mob display name (no companion mission packet needed for the snapshot).</summary>
    private void EmitList(PlayerEntity pc)
    {
        var session = _sessions?.GetByEntityId(pc.Id);
        if (session == null) return;

        var quests = new List<Core.Server.Packets.Out.ZC.AllQuestEntry>();
        foreach (var q in pc.QuestLog)
        {
            var cat = GetCatalogEntry((uint)q.QuestId);
            var objs = new List<Core.Server.Packets.Out.ZC.AllQuestObjective>();
            if (cat != null)
            {
                for (byte i = 0; i < 3; i++)
                {
                    var target = ObjectiveTarget(cat, i);
                    if (target <= 0) continue;
                    var aegis = i switch { 0 => cat.Mob1, 1 => cat.Mob2, 2 => cat.Mob3, _ => null };
                    var mob = aegis != null ? _mobDb?.GetByAegisName(aegis) : null;
                    var mobId = mob?.Id ?? 1002; // MOBID_PORING fallback (rAthena)
                    var name = mob?.Name ?? aegis ?? string.Empty;
                    var current = (short)(i < q.Counts.Length ? q.Counts[i] : 0);
                    objs.Add(new Core.Server.Packets.Out.ZC.AllQuestObjective
                    {
                        QuestIndex = q.QuestId * 1000 + i,
                        MobId = mobId,
                        Current = current,
                        Target = (short)target,
                        MobName = name,
                    });
                }
            }
            quests.Add(new Core.Server.Packets.Out.ZC.AllQuestEntry
            {
                QuestId = q.QuestId,
                State = (byte)q.State,
                TimeDiff = 0,
                Time = (uint)Math.Max(0, q.TimeUnix),
                Objectives = objs,
            });
        }

        session.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_ALL_QUEST_LIST { Quests = quests });
    }

    /// <summary>FEATURE-03 — rAthena <c>quest_update_objective_sub</c>: add <paramref name="delta"/>
    /// to objective <paramref name="index"/> of an active quest (capped at the catalog target),
    /// returning the new count, or -1 when the quest/objective isn't valid.</summary>
    public int UpdateObjectiveSub(PlayerEntity pc, int questId, byte index, int delta)
    {
        var q = FindActive(pc, questId);
        if (q == null) return -1;
        var cat = GetCatalogEntry((uint)questId);
        if (cat == null) return -1;
        var target = ObjectiveTarget(cat, index);
        if (target <= 0) return -1;
        EnsureCounts(q, index + 1);
        q.Counts[index] = Math.Clamp(q.Counts[index] + delta, 0, target);
        return q.Counts[index];
    }

    /// <summary>
    /// FEATURE-01 — rAthena <c>quest_update_objective</c> single-objective bump: add
    /// <paramref name="delta"/> to objective <paramref name="index"/> of the active quest
    /// <paramref name="questId"/> (capped at the catalog target), completing the quest when every
    /// objective is satisfied.
    /// </summary>
    public void UpdateObjective(PlayerEntity pc, int questId, byte index, int delta)
    {
        if (delta == 0) return;
        var before = FindActive(pc, questId);
        if (before == null) return;
        var prior = index < before.Counts.Length ? before.Counts[index] : 0;
        if (UpdateObjectiveSub(pc, questId, index, delta) < 0) return;
        if (before.Counts[index] == prior) return; // already capped — no change
        TryComplete(before, GetCatalogEntry((uint)questId)!);
        EmitUpdate(pc, questId);
    }

    /// <inheritdoc />
    public void UpdateMobObjective(PlayerEntity pc, string mobAegis)
    {
        if (string.IsNullOrEmpty(mobAegis)) return;
        foreach (var q in pc.QuestLog)
        {
            if (q.State != QActive) continue; // Q_ACTIVE only (rAthena skips Q_COMPLETE)
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
            if (changed)
            {
                TryComplete(q, cat);
                EmitUpdate(pc, q.QuestId); // live hunt-count tick to the client
            }
        }
    }

    /// <summary>FEATURE-03 — rAthena <c>quest_update_status</c> (quest.cpp:850): set the quest's
    /// state. Returns 0 if applied, -1 if the PC doesn't have the quest.</summary>
    public int UpdateStatus(PlayerEntity pc, int questId, byte status)
    {
        var idx = IndexOf(pc, questId);
        if (idx < 0) return -1;
        pc.QuestLog[idx].State = status;
        // PACKET-10: ZC_UPDATE_MISSION_HUNT (status<complete) / ZC_DEL_QUEST (complete) emit seam.
        return 0;
    }

    // --- client emits (GP-QUEST) ---

    /// <summary>rAthena <c>clif_quest_add</c> — tell the client a quest was accepted, with its
    /// objectives (mob id + display name + target + current). Resolves mob aegis → id/name via the
    /// mob DB; a missing mob falls back to Poring (rAthena: 0 can't be sent).</summary>
    private void EmitAdd(PlayerEntity pc, int questId)
    {
        var session = _sessions?.GetByEntityId(pc.Id);
        if (session == null) return;
        var cat = GetCatalogEntry((uint)questId);
        var idx = IndexOf(pc, questId);
        if (cat == null || idx < 0) return;
        var q = pc.QuestLog[idx];

        var primary = new List<Core.Server.Packets.Out.ZC.AddQuestObjective>();
        var mission = new List<Core.Server.Packets.Out.ZC.MissionObjective>();
        for (byte i = 0; i < 3; i++)
        {
            var target = ObjectiveTarget(cat, i);
            if (target <= 0) continue;
            var aegis = i switch { 0 => cat.Mob1, 1 => cat.Mob2, 2 => cat.Mob3, _ => null };
            var mob = aegis != null ? _mobDb?.GetByAegisName(aegis) : null;
            var mobId = mob?.Id ?? 1002; // MOBID_PORING fallback (rAthena)
            var name = mob?.Name ?? aegis ?? string.Empty;
            var current = (short)(i < q.Counts.Length ? q.Counts[i] : 0);
            var questIndex = questId * 1000 + i;
            primary.Add(new Core.Server.Packets.Out.ZC.AddQuestObjective { QuestIndex = questIndex, MobId = mobId, Current = current, MobName = name });
            mission.Add(new Core.Server.Packets.Out.ZC.MissionObjective(mobId, questIndex, (short)target, current));
        }

        session.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_ADD_QUEST
        {
            QuestId = questId,
            State = (byte)q.State,
            TimeDiff = 0,
            Time = (uint)Math.Max(0, q.TimeUnix),
            Objectives = primary,
        });
        if (mission.Count > 0)
            session.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_ADD_QUEST_MISSION { Objectives = mission });
    }

    /// <summary>rAthena <c>clif_quest_delete</c> — tell the client to drop the quest from its log.</summary>
    private void EmitDelete(PlayerEntity pc, int questId)
        => _sessions?.GetByEntityId(pc.Id)?.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_DEL_QUEST { QuestId = questId });

    /// <summary>rAthena <c>clif_quest_update_objective</c> — push the live hunt counts (target +
    /// current per objective) for an active quest so the tracker ticks up.</summary>
    private void EmitUpdate(PlayerEntity pc, int questId)
    {
        var session = _sessions?.GetByEntityId(pc.Id);
        if (session == null) return;
        var cat = GetCatalogEntry((uint)questId);
        var idx = IndexOf(pc, questId);
        if (cat == null || idx < 0) return;
        var q = pc.QuestLog[idx];

        var objs = new List<Core.Server.Packets.Out.ZC.MissionObjective>();
        for (byte i = 0; i < 3; i++)
        {
            var target = ObjectiveTarget(cat, i);
            if (target <= 0) continue;
            var current = i < q.Counts.Length ? q.Counts[i] : 0;
            objs.Add(new Core.Server.Packets.Out.ZC.MissionObjective(questId, questId * 1000 + i, (short)target, (short)current));
        }
        if (objs.Count == 0) return;
        session.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_UPDATE_MISSION_HUNT { Objectives = objs });
    }

    // --- objective + lookup helpers ---

    private static int IndexOf(PlayerEntity pc, int questId)
    {
        for (var i = 0; i < pc.QuestLog.Count; i++)
            if (pc.QuestLog[i].QuestId == questId) return i;
        return -1;
    }

    private static int ObjectiveCount(Core.Database.Entities.QuestDbEntity cat)
    {
        var n = 0;
        if (!string.IsNullOrEmpty(cat.Mob1)) n++;
        if (!string.IsNullOrEmpty(cat.Mob2)) n++;
        if (!string.IsNullOrEmpty(cat.Mob3)) n++;
        return n;
    }

    private static bool AllObjectivesMet(QuestEntry q, Core.Database.Entities.QuestDbEntity? cat)
    {
        if (cat == null) return false;
        for (byte i = 0; i < 3; i++)
        {
            var target = ObjectiveTarget(cat, i);
            if (target <= 0) continue;
            if (i >= q.Counts.Length || q.Counts[i] < target) return false;
        }
        return true;
    }

    private static QuestEntry? FindActive(PlayerEntity pc, int questId)
    {
        foreach (var q in pc.QuestLog)
            if (q.QuestId == questId && q.State == QActive) return q;
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
        q.State = QComplete;
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
