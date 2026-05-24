using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CALLHOMUN — Alchemist Call Homunculus (skill.cpp:AM_CALLHOMUN
/// arm). Spawns or wakes the caster's bound homunculus via
/// <see cref="IHomunculusService.Call"/>. Fails the cast if the
/// caller has no record yet (CreateRequest must run first); rAthena
/// behavior matches.
/// </summary>
public sealed class CallHomunculus : SkillImpl
{
    public CallHomunculus() : base(SkillIds.AM_CALLHOMUN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var called = ctx.Homunculus?.Call(pc) ?? false;
        if (!called)
        {
            ctx.Client?.BroadcastSkillFail(pc, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
        }
    }
}
