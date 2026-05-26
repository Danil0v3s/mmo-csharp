using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_ROKI_CAPRICCIO — Trouvere/Troubadour Roki Capriccio. Manual
/// port of <c>rathena-fork/src/map/skills/archer/rokicapriccio.cpp</c>.
///
/// <para>Splash debuff: every nearby BL_CHAR enemy rolls
/// SC_CONFUSION (<c>4*lv %</c>) and SC_HANDICAPSTATE_MISFORTUNE
/// (<c>5*lv %</c>, doubled when a chorus partner is within AREA_SIZE).</para>
/// </summary>
public sealed class RokiCapriccio : SkillImpl
{
    private readonly Random _rng;

    public RokiCapriccio() : base(SkillIds.TR_ROKI_CAPRICCIO) => _rng = Random.Shared;
    public RokiCapriccio(Random? rng = null) : base(SkillIds.TR_ROKI_CAPRICCIO) => _rng = rng ?? Random.Shared;

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
        var misfortuneRate = 5 * skillLevel * (partnerBoost ? 2 : 1);

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        const short splash = 6;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splash, EntityType.Mob | EntityType.Pc);
        foreach (var bl in victims)
        {
            if (bl.Id.Value == src.Id.Value) continue;
            if (_rng.Next(100) < 4 * skillLevel)
                ctx.Sc?.Start(bl, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            if (_rng.Next(100) < misfortuneRate)
                ctx.Sc?.Start(bl, StatusType.HandicapstateMisfortune, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}
