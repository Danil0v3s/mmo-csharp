using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ArchBishop;

/// <summary>
/// AB_ADORAMUS — Arch Bishop Adoramus. Mirrors
/// <c>rathena-fork/src/map/skills/archbishop/adoramus.cpp</c>.
///
/// Single-target Holy magic at (330 + 70*lv)% MATK + applies
/// <see cref="StatusType.Adoramus"/> (BLIND-like debuff) on the
/// target. Requires Blue Gemstone (cost upstream).
/// </summary>
public sealed class Adoramus : SkillImpl
{
    public Adoramus() : base(SkillIds.AB_ADORAMUS) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 330 + 70 * skillLevel;
        var dmg = Math.Max(1, matk * rate / 100);
        ctx.Damage.ApplyDamage(target, dmg, src);

        // Blind-style debuff.
        ctx.Sc?.Start(target, StatusType.Adoramus, val1: skillLevel, 0, 0, 0,
            durationMs: (15 + 5 * skillLevel) * 1000, src);
    }
}
