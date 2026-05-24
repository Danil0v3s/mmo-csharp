using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONFB — Warlock Summon Fire Ball. Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonfireball.cpp</c>. Same
/// shape as <see cref="SummonStone"/> — fills an SC_SPHERE_* slot with
/// WLS_FIRE.
/// </summary>
public sealed class SummonFireBall : SkillImpl
{
    public SummonFireBall() : base(SkillIds.WL_SUMMONFB) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: SC_SPHERE_1..5 slot management (Warlock summoned-ball element registry)
        // isn't wired through IStatusChangeService yet — WLS_FIRE ball not retained.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
