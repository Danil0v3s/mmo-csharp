using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_EXTREMITYFIST — Monk Asura Strike. Mirrors
/// <c>rathena-fork/src/map/skills/monk/extremityfist.cpp</c>.
///
/// Massive single-target hit. Drains ALL SP. Damage formula
/// approximates rAthena: <c>swing * (8*lv + 100)/100 + spBefore * 8</c>.
/// </summary>
public sealed class ExtremityFist : SkillImpl
{
    public ExtremityFist() : base(SkillIds.MO_EXTREMITYFIST) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var spBefore = src.Stats.Sp;
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var dmg = (int)Math.Clamp(swing.Total * (8 * skillLevel + 100) / 100 + spBefore * 8,
            0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, Math.Max(1, dmg), src);
        src.Stats.Sp = 0;
    }
}
