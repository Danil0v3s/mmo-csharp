using Map.Server.Entities;

namespace Map.Server.Quest;

/// <summary>
/// FEATURE-22 — narrow hook that fires an immediate quest persist on mutation, mirroring rAthena's
/// <c>chrif_save(sd, CSAVE_NORMAL)</c> calls in <c>quest_add</c>/<c>quest_change</c>/<c>quest_delete</c>/
/// <c>quest_update_status</c> (gated on <c>save_settings &amp; CHARSAVE_QUEST</c>).
///
/// This indirection exists to break the DI cycle: <see cref="Map.Server.Services.Intif.IIntifService"/>
/// depends on <see cref="IQuestService"/> (for <c>SnapshotFor</c>), so <see cref="QuestService"/> can't
/// constructor-depend on the intif service directly. The implementation resolves it lazily.
/// </summary>
public interface IQuestSaveTrigger
{
    /// <summary>Persist the character's quest log now (fire-and-forget gRPC to the char server).</summary>
    void Save(PlayerEntity pc);
}
