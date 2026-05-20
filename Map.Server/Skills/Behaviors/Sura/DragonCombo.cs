using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Sura;

/// <summary>
/// SR_DRAGONCOMBO — Sura Dragon Combo. Mirrors
/// <c>rathena-fork/src/map/skills/sura/dragoncombo.cpp</c>.
///
/// Single-target physical at (350 + 150*lv)% ATK + applies stun.
/// Combo starter for the Sura chain (Knuckle Arrow → Tiger Cannon).
/// </summary>
public sealed class DragonCombo : WeaponSkillImpl
{
    public DragonCombo() : base(SkillIds.SR_DRAGONCOMBO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 250 + 150 * skillLevel;
}
