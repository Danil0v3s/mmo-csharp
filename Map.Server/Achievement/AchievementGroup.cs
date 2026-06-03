namespace Map.Server.Achievement;

/// <summary>
/// rAthena <c>e_achievement_group</c> (achievement.hpp:20) — the objective group an
/// achievement belongs to. Only the mob-keyed groups (<see cref="Battle"/> /
/// <see cref="Taming"/>) are consumed by the FEATURE-01 mob-death observer; the rest
/// are listed for parity with the enum ordinal so the <c>byte type</c> passed to
/// <see cref="IAchievementService.UpdateObjective"/> matches rAthena's value.
/// </summary>
public enum AchievementGroup : byte
{
    None = 0,
    AddFriend,
    Adventure,
    Baby,
    Battle,        // AG_BATTLE — "kill N of mob X"
    Chatting,
    ChattingCount,
    ChattingCreate,
    ChattingDying,
    Eat,
    GetItem,
    GetZeny,
    Goal_Achieve,
    Goal_Level,
    Goal_Status,
    Job_Change,
    Marry,
    Party,
    Refine_Fail,
    Refine_Success,
    Spend_Zeny,
    Taming,        // AG_TAMING — pet-taming kill objective
    Max,
}
