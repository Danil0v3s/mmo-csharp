using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_PHANTOMMENACE — Phantom Menace. Manual port of
/// <c>rathena-fork/src/map/skills/thief/phantommenace.cpp</c>.
/// +200 ratio; splash that hits invisible / cloaked / camouflaged
/// targets. Splash + dispel are TODO — animation only.
/// </summary>
public sealed class PhantomMenace : WeaponSkillImpl
{
    public PhantomMenace() : base(SkillIds.GC_PHANTOMMENACE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;
}
