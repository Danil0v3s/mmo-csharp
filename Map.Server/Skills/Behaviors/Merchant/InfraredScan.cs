using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_INFRAREDSCAN — Mechanic Infrared Scan. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/infraredscan.cpp</c>.
/// Centred on the named target — for every enemy in a 7-cell radius,
/// dispels Hiding / Cloaking / Camouflage and applies SC_INFRAREDSCAN
/// (rAthena also dispels CLOAKINGEXCEED / NEWMOON and rolls
/// SC__SHADOWFORM by (100 - 10*lv)% — the first two are not yet
/// modelled as separate SCs, and SC__SHADOWFORM is unported).
/// </summary>
public sealed class InfraredScan : SkillImpl
{
    private const short SplashRange = 7;
    private const int DurationMs = 5000;

    public InfraredScan() : base(SkillIds.NC_INFRAREDSCAN) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillDamage(src, target, SkillId, skillLevel, 0);
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRange, EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            ctx.Sc?.End(v, StatusType.Hiding);
            ctx.Sc?.End(v, StatusType.Cloaking);
            ctx.Sc?.End(v, StatusType.Camouflage);
            ctx.Sc?.Start(v, StatusType.Infraredscan, val1: skillLevel, 0, 0, 0, durationMs: DurationMs, src);
        }
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => CastendDamageId(src, target, skillLevel, ctx);
}
