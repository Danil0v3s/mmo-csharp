using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.GuillotineCross;

/// <summary>
/// GC_DARKILLUSION — Guillotine Cross Dark Illusion. Mirrors
/// <c>rathena-fork/src/map/skills/guillotinecross/darkillusion.cpp</c>.
///
/// Teleport-and-strike: warps caster adjacent to target then
/// delivers a critical-guaranteed hit at (200 + 100*lv)% ATK.
/// </summary>
public sealed class DarkIllusion : WeaponSkillImpl
{
    public DarkIllusion() : base(SkillIds.GC_DARKILLUSION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 + 100 * skillLevel;
}
