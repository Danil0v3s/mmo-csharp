using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_DOUBLE — Mercenary Double Strafe. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_doublestrafe.cpp</c>.
/// Ratio <c>+10*(lv-1)</c>.
/// </summary>
public sealed class MercenaryDoubleStrafe : WeaponSkillImpl
{
    public MercenaryDoubleStrafe() : base(SkillIds.MA_DOUBLE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * (skillLevel - 1);
}
