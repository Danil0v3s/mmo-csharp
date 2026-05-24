using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONBL — Warlock Summon Lightning Ball
/// (skill.cpp:WL_SUMMONBL arm). Pushes a Wind Spirit Ball
/// (<c>WLS_WIND</c>) into the next free <c>SC_SPHERE_*</c> slot.
/// </summary>
public sealed class SummonLightningBall : SkillImpl
{
    private const int SphereDurationMs = 200_000;

    public SummonLightningBall() : base(SkillIds.WL_SUMMONBL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        WarlockSphereHelpers.PushSphere(src, WarlockSphereHelpers.WlsWind, skillLevel,
            SphereDurationMs, ctx);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
