using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONFB — Warlock Summon Fire Ball (skill.cpp:WL_SUMMONFB arm).
/// Pushes a Fire Spirit Ball (<c>WLS_FIRE</c>) into the next free
/// <c>SC_SPHERE_*</c> slot.
/// </summary>
public sealed class SummonFireBall : SkillImpl
{
    private const int SphereDurationMs = 200_000;

    public SummonFireBall() : base(SkillIds.WL_SUMMONFB) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        WarlockSphereHelpers.PushSphere(src, WarlockSphereHelpers.WlsFire, skillLevel,
            SphereDurationMs, ctx);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
