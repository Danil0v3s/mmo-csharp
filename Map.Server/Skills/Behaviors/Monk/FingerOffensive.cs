using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_FINGEROFFENSIVE — Monk Finger Offensive. Mirrors
/// <c>rathena-fork/src/map/skills/monk/fingeroffensive.cpp</c>.
///
/// Ranged physical at (150 + 50*lv)% ATK per hit. Hit count =
/// Spirit Spheres consumed (placeholder = skill level until the
/// sphere hook ports).
/// </summary>
public sealed class FingerOffensive : SkillImpl
{
    public FingerOffensive() : base(SkillIds.MO_FINGEROFFENSIVE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = skillLevel;
        var rate = 150 + 50 * skillLevel;
        for (var hit = 0; hit < hitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
