using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_INVESTIGATE — Monk Investigate (Occult Impaction). Mirrors
/// <c>rathena-fork/src/map/skills/monk/investigate.cpp</c>.
///
/// DEF-ignoring physical at (75 + 25*lv)% ATK. Damage scales with
/// target's DEF — the "punisher" skill for tanky targets.
/// </summary>
public sealed class Investigate : SkillImpl
{
    public Investigate() : base(SkillIds.MO_INVESTIGATE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 75 + 25 * skillLevel;
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var defFactor = Math.Max(1, (target.Stats.Def + target.Stats.Def2) / 50);
        var dmg = (int)Math.Clamp(swing.Total * rate / 100 * defFactor, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, Math.Max(1, dmg), src);
    }
}
