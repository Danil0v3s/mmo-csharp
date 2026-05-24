using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONWB — Warlock Summon Water Ball (skill.cpp:WL_SUMMONWB arm).
/// Pushes a Water Spirit Ball (<c>WLS_WATER</c>) into the next free
/// <c>SC_SPHERE_*</c> slot.
/// </summary>
public sealed class SummonWaterBall : SkillImpl
{
    private const int SphereDurationMs = 200_000;

    public SummonWaterBall() : base(SkillIds.WL_SUMMONWB) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        WarlockSphereHelpers.PushSphere(src, WarlockSphereHelpers.WlsWater, skillLevel,
            SphereDurationMs, ctx);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
