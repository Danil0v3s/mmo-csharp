using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_MARIONETTE — Clown/Gypsy Marionette Control. Manual port of
/// <c>rathena-fork/src/map/skills/archer/marionettecontrol.cpp</c>.
///
/// <para>Pairs caster (SC_MARIONETTE) with target (SC_MARIONETTE2),
/// or unpairs if already linked. Same-class same-gender Bard/Dancer
/// pairings and Curse/Quagmire'd targets reject — class detection
/// gates TODO; we apply the SC pair unconditionally on player-to-
/// player.</para>
/// </summary>
public sealed class MarionetteControl : SkillImpl
{
    public MarionetteControl() : base(SkillIds.CG_MARIONETTE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity || target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        const int duration = 60_000;
        ctx.Sc?.Start(src, StatusType.Marionette, val1: (int)target.Id, 0, 0, 0, durationMs: duration, src);
        ctx.Sc?.Start(target, StatusType.Marionette2, val1: (int)src.Id, 0, 0, 0, durationMs: duration, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
