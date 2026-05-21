using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_AIMEDBOLT — Ranger Aimed Bolt. Manual port of
/// <c>rathena-fork/src/map/skills/archer/aimedbolt.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 500 + 20*lv)</c> baseline, with
/// SC_FEARBREEZE swapping to <c>+(-100 + 800 + 35*lv)</c>. Caster SC
/// readback isn't surfaced to this hook — Fear Breeze branch TODO.</para>
/// </summary>
public sealed class AimedBolt : WeaponSkillImpl
{
    public AimedBolt() : base(SkillIds.RA_AIMEDBOLT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 500 + 20 * skillLevel);
    }
}
