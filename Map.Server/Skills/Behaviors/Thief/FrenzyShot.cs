using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_FRENZY_SHOT — Frenzy Shot. Manual port of
/// <c>rathena-fork/src/map/skills/thief/frenzyshot.cpp</c>.
/// Ratio <c>+(-100 + 250 + 800*lv) + 15*con</c>. Triple-hit chance
/// at <c>5*lv</c>% is handled in ModifyDamageData (TODO).
/// </summary>
public sealed class FrenzyShot : WeaponSkillImpl
{
    public FrenzyShot() : base(SkillIds.ABC_FRENZY_SHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 800 * skillLevel) + 15 * src.Stats.Con;
}
