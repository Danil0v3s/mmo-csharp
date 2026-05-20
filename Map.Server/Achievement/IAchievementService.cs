using Map.Server.Entities;

namespace Map.Server.Achievement;

/// <summary>
/// Achievement system. Canonical entry points for rAthena
/// <c>achievement.cpp</c> (1 219 lines, 20 functions).
/// </summary>
public interface IAchievementService
{
    /// <summary>rAthena <c>achievement_check_condition</c>.</summary>
    bool CheckCondition(PlayerEntity pc, int achievementId);
    /// <summary>rAthena <c>achievement_check_dependent</c>.</summary>
    bool CheckDependent(PlayerEntity pc, int achievementId);
    /// <summary>rAthena <c>achievement_remove</c>.</summary>
    bool Remove(PlayerEntity pc, int achievementId);
    /// <summary>rAthena <c>achievement_update_achievement</c>.</summary>
    bool UpdateAchievement(PlayerEntity pc, int achievementId, bool completed);
    /// <summary>rAthena <c>achievement_check_progress</c>.</summary>
    int CheckProgress(PlayerEntity pc, int achievementId);
    /// <summary>rAthena <c>achievement_update_objective_sub</c>.</summary>
    int UpdateObjectiveSub(PlayerEntity pc, int achievementId, byte objective, int delta);
    /// <summary>rAthena <c>achievement_update_objective</c>.</summary>
    void UpdateObjective(PlayerEntity pc, byte type, byte index, int value);
    /// <summary>rAthena <c>achievement_check_reward</c>.</summary>
    void CheckReward(PlayerEntity pc, int achievementId);
    /// <summary>rAthena <c>achievement_get_reward</c>.</summary>
    void GetReward(PlayerEntity pc, int achievementId);
    /// <summary>rAthena <c>achievement_get_titles</c>.</summary>
    IReadOnlyList<int> GetTitles(PlayerEntity pc);
    /// <summary>rAthena <c>achievement_free</c>.</summary>
    void Free(PlayerEntity pc);
    /// <summary>rAthena <c>achievement_db_reload</c>.</summary>
    void ReloadDb();
    /// <summary>rAthena <c>achievement_level</c>.</summary>
    int Level(PlayerEntity pc);
    /// <summary>rAthena <c>AchievementDatabase::mobexists</c>.</summary>
    bool MobExists(int mobId);
}
