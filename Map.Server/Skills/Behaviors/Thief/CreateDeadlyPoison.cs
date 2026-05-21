using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_CDP — Create Deadly Poison. Manual port of
/// <c>rathena-fork/src/map/skills/thief/createdeadlypoison.cpp</c>.
/// Produces a Poison Bottle (ITEMID_POISON_BOTTLE) via skill_produce_mix.
/// Production wiring is TODO — animation only.
/// </summary>
public sealed class CreateDeadlyPoison : SkillImpl
{
    public CreateDeadlyPoison() : base(SkillIds.ASC_CDP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: skill_produce_mix(sd, ASC_CDP, ITEMID_POISON_BOTTLE, …, qty=1).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
