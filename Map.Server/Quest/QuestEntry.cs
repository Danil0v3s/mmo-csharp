namespace Map.Server.Quest;

/// <summary>
/// Per-PC active quest entry. Runtime mirror of rAthena
/// <c>struct quest</c> in <c>quest.hpp</c>:
/// <code>
/// struct quest {
///     int quest_id;
///     uint count[MAX_QUEST_OBJECTIVES];
///     time_t time;
///     e_quest_state state;
/// };
/// </code>
/// Lives on <see cref="Map.Server.Entities.PlayerEntity.QuestLog"/>.
/// T7.1 added it so <see cref="IQuestService.SnapshotFor"/> can
/// serialize a real per-objective payload onto the
/// <c>QuestSaveAsync</c> RPC.
/// </summary>
public sealed class QuestEntry
{
    /// <summary>quest_db row id.</summary>
    public int QuestId { get; set; }

    /// <summary>Per-objective counters (rAthena <c>count[MAX_QUEST_OBJECTIVES]</c>).</summary>
    public int[] Counts { get; set; } = System.Array.Empty<int>();

    /// <summary>Unix timestamp at which the quest expires (0 = no deadline).</summary>
    public long TimeUnix { get; set; }

    /// <summary>rAthena <c>e_quest_state</c> — 0 inactive, 1 active, 2 complete.</summary>
    public int State { get; set; }
}
