using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_NET_REPAIR — ABR Net Repair. Manual port of
/// <c>rathena-fork/src/map/skills/other/netrepair.cpp</c>.
/// Splash heal of 10% MaxHP to allies. Splash dispatch is TODO; we
/// heal the named target.
/// </summary>
public sealed class NetRepair : SkillImpl
{
    public NetRepair() : base(SkillIds.ABR_NET_REPAIR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var heal = target.Stats.MaxHp * 10 / 100;
        if (target is PlayerEntity p)
            p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
        else if (target is MobEntity m)
            m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
    }
}
