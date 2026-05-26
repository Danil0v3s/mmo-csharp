using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_NIPELHEIM_REQUIEM — Trouvere Nipelheim Requiem. Manual port of
/// <c>rathena-fork/src/map/skills/archer/nipelheimrequiem.cpp</c>.
///
/// <para>Splash debuff: every nearby BL_CHAR enemy rolls SC_CURSE
/// (<c>4*lv %</c>) and SC_HANDICAPSTATE_DEPRESSION (<c>5*lv %</c>,
/// doubled when a chorus partner is within AREA_SIZE).</para>
/// </summary>
public sealed class NipelheimRequiem : SkillImpl
{
    private readonly Random _rng;

    public NipelheimRequiem() : base(SkillIds.TR_NIPELHEIM_REQUIEM) => _rng = Random.Shared;
    public NipelheimRequiem(Random? rng = null) : base(SkillIds.TR_NIPELHEIM_REQUIEM) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var partnerBoost = false;
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMapInRange(pcSrc, 14, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                partnerBoost = true;
            }, includeSelf: false);
        }
        var depRate = 5 * skillLevel * (partnerBoost ? 2 : 1);

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        const short splash = 6;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splash, EntityType.Mob | EntityType.Pc);
        foreach (var bl in victims)
        {
            if (bl.Id.Value == src.Id.Value) continue;
            if (_rng.Next(100) < 4 * skillLevel)
                ctx.Sc?.Start(bl, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            if (_rng.Next(100) < depRate)
                ctx.Sc?.Start(bl, StatusType.HandicapstateDepression, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}
