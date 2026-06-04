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
    /// <summary>GP-ACHIEVE — rAthena <c>achievement_data.total_score</c>.</summary>
    int TotalScore(PlayerEntity pc);
    /// <summary>GP-ACHIEVE — rAthena <c>achievement_level</c>: level + bar exp/expNext + total score.</summary>
    (int Level, int Exp, int ExpNext, int TotalScore) LevelInfo(PlayerEntity pc);
    /// <summary>rAthena <c>AchievementDatabase::mobexists</c>.</summary>
    bool MobExists(int mobId);

    /// <summary>GP-ACHIEVE — rAthena <c>clif_achievement_list_all</c>: push the full achievement window on
    /// login (called after the achievement log is hydrated).</summary>
    void PcLogin(PlayerEntity pc);
    /// <summary>GP-ACHIEVE — rAthena <c>clif_achievement_update</c>: emit one achievement's progress.</summary>
    void EmitUpdate(PlayerEntity pc, int achievementId);
    /// <summary>GP-ACHIEVE — rAthena <c>clif_parse_change_title</c>: equip an earned achievement title
    /// (or clear it with id ≤ 0). Returns true when applied. Emits <c>ZC_ACK_CHANGE_TITLE</c>.</summary>
    bool SetTitle(PlayerEntity pc, int titleId);

    /// <summary>
    /// T7.1 — serialize the PC's achievement progress into the gRPC
    /// payload shape consumed by <c>AchievementSaveAsync</c>. Reads
    /// <see cref="Map.Server.Entities.PlayerEntity.AchievementLog"/>.
    /// Char-side persists via upsert (rAthena
    /// <c>mapif_parse_AchievementSave</c>), so empty list = leave the
    /// existing rows alone (different from Quest, which DELETEs).
    /// </summary>
    IReadOnlyList<Core.Server.IPC.AchievementEntryData> SnapshotFor(PlayerEntity pc);

    /// <summary>
    /// T7.1 — hydrate the PC's achievement log from a load-response
    /// payload. Replaces any existing entries.
    /// </summary>
    void Hydrate(PlayerEntity pc, IEnumerable<Core.Server.IPC.AchievementEntryData> entries);
}
