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
        // TODO: SC_SPHERE_1..5 slot management with WLS_FIRE element.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
