using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Paladin;

/// <summary>
/// PA_PRESSURE — Paladin Pressure. Mirrors
/// <c>rathena-fork/src/map/skills/paladin/pressure.cpp</c>.
///
/// Fixed-damage hit (lv*800) regardless of weapon — bypasses
/// DEF entirely. The cast consumes 8% SP and one Blue Gemstone
/// (cost gating lives upstream).
/// </summary>
public sealed class Pressure : SkillImpl
{
    public Pressure() : base(SkillIds.PA_PRESSURE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var dmg = 800 * skillLevel;
        ctx.Damage.ApplyDamage(target, dmg, src);
    }
}
