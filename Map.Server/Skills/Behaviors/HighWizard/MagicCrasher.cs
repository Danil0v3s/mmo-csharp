using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.HighWizard;

/// <summary>
/// HW_MAGICCRASHER — High Wizard Magic Crasher. Mirrors
/// <c>rathena-fork/src/map/skills/highwizard/magiccrasher.cpp</c>.
///
/// Single-target weapon-attack-as-magic: deals weapon ATK damage
/// at the caster's weapon element via the magic resolver path.
/// In renewal: skill_db handles the ratio; this plugin just claims
/// the cast for special treatment when the weapon-element-as-magic
/// dispatch ports.
///
/// Approximation: applies MATK midpoint at 100 %.
/// </summary>
public sealed class MagicCrasher : SkillImpl
{
    public MagicCrasher() : base(SkillIds.HW_MAGICCRASHER) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        ctx.Damage.ApplyDamage(target, matk, src);
    }
}
