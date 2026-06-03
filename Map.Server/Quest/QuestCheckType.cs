namespace Map.Server.Quest;

/// <summary>
/// rAthena <c>e_quest_check_type</c> (quest.hpp:52) — the query mode for
/// <see cref="IQuestService.Check"/>.
/// </summary>
public enum QuestCheckType : byte
{
    /// <summary>Query the quest's state (Q_INACTIVE reported as active).</summary>
    HaveQuest = 0,
    /// <summary>2 if the time limit has expired, 1 if completed, 0 otherwise.</summary>
    PlayTime = 1,
    /// <summary>2 if all objectives are met, 1 if the time limit expired, 0 otherwise.</summary>
    Hunting = 2,
}
