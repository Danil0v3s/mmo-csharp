using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_POISON_SHIELD — Elemental Poison Shield. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/poisonshield.cpp</c>.
/// Toggles SC_POISON_SHIELD on target + SC_POISON_SHIELD_OPTION on
/// the elemental.
/// </summary>
public sealed class PoisonShield : SkillImpl
{
    public PoisonShield() : base(SkillIds.EM_EL_POISON_SHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.PoisonShield) != null
            || ctx.Sc?.Get(src, StatusType.PoisonShieldOption) != null)
        {
            ctx.Sc.End(target, StatusType.PoisonShield);
            ctx.Sc.End(src, StatusType.PoisonShieldOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.PoisonShieldOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.PoisonShield, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
