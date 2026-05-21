using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_HELLS_DRIVE — Hyper Novice Hell's Drive. Manual port of
/// <c>rathena-fork/src/map/skills/novice/hellsdrive.cpp</c>.
/// Ratio <c>+(-100 + 1700 + 900*lv) + 3*SPL</c>. HN_SELFSTUDY_SOCERY
/// amplifier + SC_RULEBREAK boost are TODO.
/// </summary>
public sealed class HellsDrive : SkillImpl
{
    public HellsDrive() : base(SkillIds.HN_HELLS_DRIVE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1700 + 900 * skillLevel) + 3 * src.Stats.Spl;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
