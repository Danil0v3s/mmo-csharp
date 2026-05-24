using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_ACTION — Sorcerer Elemental Action (skill.cpp:SO_EL_ACTION
/// arm). Refuses if the caster has no bound elemental
/// (<see cref="PlayerEntity.ActiveElementalClassId"/> = 0); otherwise
/// asks <see cref="Map.Server.Elemental.IElementalService.Action"/> to
/// dispatch the elemental's attack at the target. rAthena also
/// schedules a re-action cooldown via <c>skill_blockpc_start</c>
/// (3 s × tier) — the per-tier block isn't on a hook today and is
/// flagged separately on the skill-block service.
/// </summary>
public sealed class ElementalAction : SkillImpl
{
    public ElementalAction() : base(SkillIds.SO_EL_ACTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.ActiveElementalClassId == 0) return;
        ctx.Elemental?.Action(pc, target.Id, Environment.TickCount64);
    }
}
