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
        if (target is PlayerEntity && ctx.Sc?.Get(target, StatusType.Changeundead) != null)
        {
            // TODO: skill_attack(BF_MISC) when target is undead-flagged player with HP > 1.
            return;
        }
        ctx.Sc?.Start(target, StatusType.Blessing, val1: skillLevel, 0, 0, 0, durationMs: 120_000, src);
    }
}
