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
}
