using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_KAHU_ENTEN — Fire Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/firecharm.cpp</c>.
/// Adds a fire spirit charm (element FIRE) to the player up to
/// MAX_SPIRITCHARM (10), with 300000ms duration from skill_db.
/// </summary>
public sealed class FireCharm : SkillImpl
{
    // rAthena ELE_FIRE = CHARM_TYPE_FIRE = 3.
    private const int ElementFire = 3;
    // skill_db.yml Duration1 (300000 ms).
    private const int CharmDurationMs = 300_000;
    private const int MaxSpiritCharm = 10;

    public FireCharm() : base(SkillIds.KO_KAHU_ENTEN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity sd)
            ctx.Orbs?.AddCharm(sd, ElementFire, MaxSpiritCharm, CharmDurationMs);
    }
}
