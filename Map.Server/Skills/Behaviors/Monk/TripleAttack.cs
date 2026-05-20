using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_TRIPLEATTACK — Monk Raging Triple Blow. Mirrors
/// <c>rathena-fork/src/map/skills/monk/tripleattack.cpp</c>.
///
/// 3-hit physical combo opener. Total ratio (110 + 30*lv)% split
/// across 3 hits.
/// </summary>
public sealed class TripleAttack : SkillImpl
{
    private const int HitCount = 3;

    public TripleAttack() : base(SkillIds.MO_TRIPLEATTACK) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var totalRate = 110 + 30 * skillLevel;
        var perHitRate = totalRate / HitCount;
        for (var hit = 0; hit < HitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * perHitRate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
