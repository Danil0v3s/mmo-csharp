using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ShadowChaser;

/// <summary>
/// SC_TRIANGLESHOT — Shadow Chaser Triangle Shot. Mirrors
/// <c>rathena-fork/src/map/skills/shadowchaser/triangleshot.cpp</c>.
///
/// 3-hit ranged physical at (230 + 50*lv) % ATK per hit. Consumes
/// one arrow per cast.
/// </summary>
public sealed class TriangleShot : SkillImpl
{
    private const int HitCount = 3;

    public TriangleShot() : base(SkillIds.SC_TRIANGLESHOT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 230 + 50 * skillLevel;
        for (var hit = 0; hit < HitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
