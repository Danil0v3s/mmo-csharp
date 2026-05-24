using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONSTONE — Warlock Summon Stone (skill.cpp:WL_SUMMONSTONE
/// arm). Pushes an Earth Spirit Ball (<c>WLS_STONE</c>) into the next
/// free <c>SC_SPHERE_*</c> slot via <see cref="WarlockSphereHelpers.PushSphere"/>.
/// Lv 2+ overwrites the oldest slot when all five are full.
/// </summary>
public sealed class SummonStone : SkillImpl
{
    private const int SphereDurationMs = 200_000;

    public SummonStone() : base(SkillIds.WL_SUMMONSTONE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        WarlockSphereHelpers.PushSphere(src, WarlockSphereHelpers.WlsStone, skillLevel,
            SphereDurationMs, ctx);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
