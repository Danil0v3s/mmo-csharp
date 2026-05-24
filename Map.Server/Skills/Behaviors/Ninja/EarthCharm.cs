using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_DOHU_KOUKAI — Earth Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/earthcharm.cpp</c>.
/// Adds an earth spirit charm (element EARTH) to the player up to
/// MAX_SPIRITCHARM (10), with 300000ms duration from skill_db.
/// </summary>
public sealed class EarthCharm : SkillImpl
{
    // rAthena ELE_EARTH = CHARM_TYPE_LAND = 2.
    private const int ElementEarth = 2;
    // skill_db.yml Duration1 (300000 ms).
    private const int CharmDurationMs = 300_000;
    private const int MaxSpiritCharm = 10;

    public EarthCharm() : base(SkillIds.KO_DOHU_KOUKAI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity sd)
            ctx.Orbs?.AddCharm(sd, ElementEarth, MaxSpiritCharm, CharmDurationMs);
    }
}
