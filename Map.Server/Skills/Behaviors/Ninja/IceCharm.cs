using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_HYOUHU_HUBUKI — Ice Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/icecharm.cpp</c>.
/// Adds a water spirit charm (element WATER) to the player up to
/// MAX_SPIRITCHARM (10), with 300000ms duration from skill_db.
/// </summary>
public sealed class IceCharm : SkillImpl
{
    // rAthena ELE_WATER = CHARM_TYPE_WATER = 1.
    private const int ElementWater = 1;
    // skill_db.yml Duration1 (300000 ms).
    private const int CharmDurationMs = 300_000;
    private const int MaxSpiritCharm = 10;

    public IceCharm() : base(SkillIds.KO_HYOUHU_HUBUKI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity sd)
            ctx.Orbs?.AddCharm(sd, ElementWater, MaxSpiritCharm, CharmDurationMs);
    }
}
