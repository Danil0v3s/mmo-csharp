using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_NET_SUPPORT — ABR Net Support. Manual port of
/// <c>rathena-fork/src/map/skills/other/netsupport.cpp</c>.
/// Splash heal of 3% MaxSP to allies. Splash dispatch is TODO; we
/// heal the named target (players only).
/// </summary>
public sealed class NetSupport : SkillImpl
{
    public NetSupport() : base(SkillIds.ABR_NET_SUPPORT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity p)
        {
            var heal = p.MaxSp * 3 / 100;
            p.Sp = Math.Min(p.MaxSp, p.Sp + heal);
        }
    }
}
