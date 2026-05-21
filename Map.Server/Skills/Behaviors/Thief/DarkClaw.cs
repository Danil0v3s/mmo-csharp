using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_DARKCROW — Dark Claw / Dark Crow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/darkclaw.cpp</c>.
/// Ratio <c>+100*(lv-1)</c>. Applies SC_DARKCROW even on miss
/// (handled in additional-effect chain — TODO).
/// </summary>
public sealed class DarkClaw : WeaponSkillImpl
{
    public DarkClaw() : base(SkillIds.GC_DARKCROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
