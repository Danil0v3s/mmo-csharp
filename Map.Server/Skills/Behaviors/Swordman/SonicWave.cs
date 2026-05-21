using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_SONICWAVE — Rune Knight Sonic Wave. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/sonicwave.cpp</c>.
/// Ratio <c>+(-100 + 1050 + 150*lv)</c>; hit chance bonus <c>+3*lv%</c>.
/// </summary>
public sealed class SonicWave : WeaponSkillImpl
{
    public SonicWave() : base(SkillIds.RK_SONICWAVE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1050 + 150 * skillLevel);

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 3 * skillLevel / 100);
}
