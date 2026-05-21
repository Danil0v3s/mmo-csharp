using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_INFRAREDSCAN — Mechanic Infrared Scan. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/infraredscan.cpp</c>.
/// Splash dispel of all hide/cloak/camouflage/newmoon SCs and apply
/// SC_INFRAREDSCAN. Splash dispatch TODO; we land the dispel + SC on
/// the named target.
/// </summary>
public sealed class InfraredScan : SkillImpl
{
    public InfraredScan() : base(SkillIds.NC_INFRAREDSCAN) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Hiding);
        ctx.Sc?.End(target, StatusType.Cloaking);
        ctx.Sc?.End(target, StatusType.Camouflage);
        ctx.Sc?.Start(target, StatusType.Infraredscan, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => CastendDamageId(src, target, skillLevel, ctx);
}
