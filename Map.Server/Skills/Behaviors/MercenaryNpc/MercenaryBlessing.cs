using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_BLESSING — Mercenary Blessing. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_blessing.cpp</c>.
/// On SC_CHANGEUNDEAD player targets the skill deals miscellaneous
/// damage instead of buffing. Otherwise starts SC_BLESSING.
/// </summary>
public sealed class MercenaryBlessing : SkillImpl
{
    public MercenaryBlessing() : base(SkillIds.MER_BLESSING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity pc && ctx.Sc?.Get(target, StatusType.Changeundead) != null)
        {
            // rAthena mercenary_blessing.cpp:18 — on undead-flagged PC
            // with hp > 1, dispatch as BF_MISC damage rather than buff.
            // skill_attack handles the floor-to-(hp-1) clamp so the hit
            // never kills the target.
            if (pc.Hp > 1)
            {
                ctx.SkillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
            }
            return;
        }
        ctx.Sc?.Start(target, StatusType.Blessing, val1: skillLevel, 0, 0, 0, durationMs: 120_000, src);
    }
}
