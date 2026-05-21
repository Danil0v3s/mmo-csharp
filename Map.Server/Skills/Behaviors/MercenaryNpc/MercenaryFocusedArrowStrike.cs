using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_SHARPSHOOTING — Mercenary Focused Arrow Strike. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_focusedarrowstrike.cpp</c>.
/// Renewal: ratio <c>+(-100 + 300 + 300*lv)</c>.
/// </summary>
public sealed class MercenaryFocusedArrowStrike : SkillImpl
{
    public MercenaryFocusedArrowStrike() : base(SkillIds.MA_SHARPSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 + 300 * skillLevel);
}
