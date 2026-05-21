using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_KYOUGAKU — Illusion Shock. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/illusionshock.cpp</c>.
/// Status-only buff; gated by <c>rnd()%100 &lt; targetInt/2</c>.
/// Otherwise fail.
/// </summary>
public sealed class IllusionShock : StatusSkillImpl
{
    public IllusionShock() : base(SkillIds.KO_KYOUGAKU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < target.Stats.IntStat / 2)
        {
            base.CastendNoDamageId(src, target, skillLevel, ctx);
            return;
        }
        if (src is PlayerEntity sd)
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
    }
}
