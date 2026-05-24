using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CALLHOMUN — Alchemist Call Homunculus. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/callhomunculus.cpp</c>.
/// Spawns or recalls the caster's bound homunculus. hom_call service
/// not wired — broadcast only.
/// </summary>
public sealed class CallHomunculus : SkillImpl
{
    public CallHomunculus() : base(SkillIds.AM_CALLHOMUN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: homunculus subsystem (IHomunculusService.Call) is not ported.
    }
}
