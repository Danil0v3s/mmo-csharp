using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_VENOMPRESSURE — Venom Pressure. Manual port of
/// <c>rathena-fork/src/map/skills/thief/venompressure.cpp</c>.
/// +900 ratio. +10 + 4*lv hit-rate boost.
/// </summary>
public sealed class VenomPressure : WeaponSkillImpl
{
    public VenomPressure() : base(SkillIds.GC_VENOMPRESSURE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + 10 + 4 * skillLevel);
}
