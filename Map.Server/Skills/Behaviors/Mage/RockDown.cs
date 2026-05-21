using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_ROCK_DOWN — Arch Mage Rock Down. Manual port of
/// <c>rathena-fork/src/map/skills/mage/rockdown.cpp</c>.
///
/// <para>Earth splash. Ratio: <c>+(-100 + 1550*lv) + 5*SPL</c>; +<c>300*lv</c>
/// when SC_CLIMAX is active on caster (SC readback TODO).</para>
/// </summary>
public sealed class RockDown : RecursiveDamageSplashSkillImpl
{
    public RockDown() : base(SkillIds.AG_ROCK_DOWN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 1550 * skillLevel) + 5 * src.Stats.Spl;
    }
}
