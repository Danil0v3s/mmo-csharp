using Map.Server.Entities;

namespace Map.Server.Quest;

/// <summary>
/// Quest log + objective tracking. Canonical entry points for
/// rAthena <c>quest.cpp</c> (995 lines, 12 functions).
/// </summary>
public interface IQuestService
{
    /// <summary>rAthena <c>quest_add</c>.</summary>
    int Add(PlayerEntity pc, int questId);

    /// <summary>rAthena <c>quest_change</c>.</summary>
    int Change(PlayerEntity pc, int oldQuestId, int newQuestId);

    /// <summary>rAthena <c>quest_check</c>.</summary>
    int Check(PlayerEntity pc, int questId, byte status);

    /// <summary>rAthena <c>quest_delete</c>.</summary>
    int Delete(PlayerEntity pc, int questId);

    /// <summary>rAthena <c>quest_pc_login</c> — hydrate quest log at login.</summary>
    int PcLogin(PlayerEntity pc);

    /// <summary>rAthena <c>quest_update_objective_sub</c>.</summary>
    int UpdateObjectiveSub(PlayerEntity pc, int questId, byte index, int delta);

    /// <summary>rAthena <c>quest_update_objective</c>.</summary>
    void UpdateObjective(PlayerEntity pc, int questId, byte index, int delta);

    /// <summary>rAthena <c>quest_update_status</c>.</summary>
    int UpdateStatus(PlayerEntity pc, int questId, byte status);

    /// <summary>rAthena <c>QuestDatabase::parseBodyNode</c>.</summary>
    void Reload();

    /// <summary>
    /// T7.1 — serialize the PC's active quest log into the gRPC
    /// payload shape consumed by <c>QuestSaveAsync</c>. Reads
    /// <see cref="Map.Server.Entities.PlayerEntity.QuestLog"/>.
    /// Empty list = no quests; the char-side write is a DELETE-then-
    /// INSERT against the <c>quest</c> table (rAthena parity), so
    /// returning empty wipes the row set rather than no-ops it —
    /// only call this after the PC's quest state has been hydrated.
    /// </summary>
    IReadOnlyList<Core.Server.IPC.QuestEntryData> SnapshotFor(PlayerEntity pc);

    /// <summary>
    /// T7.1 — hydrate the PC's quest log from a load-response payload.
    /// Called once per session enter after <c>QuestLoadAsync</c>
    /// returns. Replaces any existing entries.
    /// </summary>
    void Hydrate(PlayerEntity pc, IEnumerable<Core.Server.IPC.QuestEntryData> entries);
}
