using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_FIGHTINGSPIRIT — Rune Knight Fighting Spirit. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/fightingspirit.cpp</c>.
/// Val1 = ATK bonus (70 + 7*runelv), Val2 = ASPD bonus (4*runelv).
/// runelv = RK_RUNEMASTERY level (defaults to 10 here pending skill tree).
/// </summary>
public sealed class FightingSpirit : SkillImpl
{
    public FightingSpirit() : base(SkillIds.RK_FIGHTINGSPIRIT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: RK_RUNEMASTERY skill id is not yet registered in SkillIds;
        // once it lands, replace the constant with
        // ctx.PlayerSkill?.CheckSkill(src as PlayerEntity, SkillIds.RK_RUNEMASTERY) ?? 10.
        const int runeLv = 10;
        ctx.Sc?.Start(target, StatusType.Fightingspirit, val1: 70 + 7 * runeLv, val2: 4 * runeLv, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
