using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_M_MACHINE — Meister Manufacture Machine. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/manufacturemachine.cpp</c>.
/// Opens the Meister crafting panel. UI packet TODO.
/// </summary>
public sealed class ManufactureMachine : SkillImpl
{
    public ManufactureMachine() : base(SkillIds.MT_M_MACHINE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
