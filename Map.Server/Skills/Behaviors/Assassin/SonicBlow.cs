using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Assassin;

/// <summary>
/// AS_SONICBLOW — Assassin Sonic Blow. Mirrors
/// <c>rathena-fork/src/map/skills/assassin/sonicblow.cpp</c>.
///
/// 8-hit chain on single target. Total ratio (300 + 40*lv)% split
/// across 8 hits. Requires katar (gated upstream).
/// </summary>
public sealed class SonicBlow : SkillImpl
{
    private const int HitCount = 8;

    public SonicBlow() : base(SkillIds.AS_SONICBLOW) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var totalRate = 300 + 40 * skillLevel;
        var perHitRate = totalRate / HitCount;
        for (var hit = 0; hit < HitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * perHitRate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
