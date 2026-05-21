using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_DISSONANCE — Bard Unchained Serenade (Dissonance). Manual port
/// of <c>rathena-fork/src/map/skills/archer/unchainedserenade.cpp</c>.
/// Renewal damage ratio <c>+(10 + 50*lv)</c>. Job-level scale TODO.
/// </summary>
public sealed class UnchainedSerenade : WeaponSkillImpl
{
    public UnchainedSerenade() : base(SkillIds.BA_DISSONANCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 + skillLevel * 50;
}
