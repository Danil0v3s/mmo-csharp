using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_INCAGI — Mercenary Increase Agility. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_increaseagility.cpp</c>.
/// On SC_CHANGEUNDEAD player targets, deals damage. Otherwise applies
/// SC_INCREASEAGI.
/// </summary>
public sealed class MercenaryIncreaseAgility : SkillImpl
{
    public MercenaryIncreaseAgility() : base(SkillIds.MER_INCAGI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity pc && ctx.Sc?.Get(target, StatusType.Changeundead) != null)
        {
            // rAthena mercenary_increaseagility.cpp:18 — undead-flagged PC
            // with hp > 1 takes BF_MISC damage instead of getting the buff.
            // The hp-floor-to-1 clamp lives in SkillAttackService so the
            // dispatch is identical to the rAthena skill_attack path.
            if (pc.Hp > 1)
            {
                ctx.SkillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
            }
            return;
        }
        ctx.Sc?.Start(target, StatusType.IncreaseAgi, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
