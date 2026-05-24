using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_KAZEHU_SEIRAN — Wind Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/windcharm.cpp</c>.
/// Adds a wind spirit charm (element WIND) to the player up to
/// MAX_SPIRITCHARM (10), with 300000ms duration from skill_db.
/// </summary>
public sealed class WindCharm : SkillImpl
{
    // rAthena ELE_WIND = CHARM_TYPE_WIND = 4.
    private const int ElementWind = 4;
    // skill_db.yml Duration1 (300000 ms).
    private const int CharmDurationMs = 300_000;
    private const int MaxSpiritCharm = 10;

    public WindCharm() : base(SkillIds.KO_KAZEHU_SEIRAN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity sd)
            ctx.Orbs?.AddCharm(sd, ElementWind, MaxSpiritCharm, CharmDurationMs);
    }
}
