using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_TRIPLEATTACK — Monk Raging Trifecta Blow. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ragingtrifectablow.cpp</c>.
///
/// <para>3-hit melee Monk combo. Ratio: <c>+20 * skillLevel</c>
/// on top of the base. Just a simple per-level damage bump on
/// the standard weapon-hit pipeline.</para>
/// </summary>
public sealed class RagingTrifectaBlow : WeaponSkillImpl
{
    public RagingTrifectaBlow() : base(SkillIds.MO_TRIPLEATTACK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 20 * skillLevel;
    }
}
