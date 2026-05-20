using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.LordKnight;

/// <summary>
/// LK_SPIRALPIERCE — Lord Knight Spiral Pierce. Mirrors
/// <c>rathena-fork/src/map/skills/lordknight/spiralpierce.cpp</c>.
///
/// 5-hit chain weapon attack scaling with weapon weight. Damage
/// approximation: each hit (50 + 25*lv)% ATK with the spiral hit
/// count fixed at 5 (rAthena: actual ratio includes weapon weight
/// table that we approximate at flat for the first port).
/// </summary>
public sealed class SpiralPierce : SkillImpl
{
    private const int HitCount = 5;

    public SpiralPierce() : base(SkillIds.LK_SPIRALPIERCE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 50 + 25 * skillLevel;
        for (var hit = 0; hit < HitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
