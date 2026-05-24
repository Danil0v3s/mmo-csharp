using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONWB — Warlock Summon Water Ball. Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonwaterball.cpp</c>. Same
/// shape as <see cref="SummonStone"/> — fills an SC_SPHERE_* slot with
/// WLS_WATER.
/// </summary>
public sealed class SummonWaterBall : SkillImpl
{
    public SummonWaterBall() : base(SkillIds.WL_SUMMONWB) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: SC_SPHERE_1..5 slot management (Warlock summoned-ball element registry)
        // isn't wired through IStatusChangeService yet — WLS_WATER ball not retained.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
