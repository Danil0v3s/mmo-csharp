using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_TRIPLEACTION — Gunslinger Triple Action. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/tripleaction.cpp</c>.
/// Ratio <c>+50*lv</c>.
/// </summary>
public sealed class TripleAction : WeaponSkillImpl
{
    public TripleAction() : base(SkillIds.GS_TRIPLEACTION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
