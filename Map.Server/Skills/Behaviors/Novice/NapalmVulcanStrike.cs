using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_NAPALM_VULCAN_STRIKE — Hyper Novice Napalm Vulcan Strike. Manual
/// port of <c>rathena-fork/src/map/skills/novice/napalmvulcanstrike.cpp</c>.
/// Ratio <c>+(-100 + 350 + 650*lv) + 3*SPL</c>. 5*lv% chance to curse on
/// hit. HN_SELFSTUDY_SOCERY amp + SC_RULEBREAK boost are TODO.
/// </summary>
public sealed class NapalmVulcanStrike : SkillImpl
{
    private readonly Random _rng;

    public NapalmVulcanStrike() : base(SkillIds.HN_NAPALM_VULCAN_STRIKE) => _rng = Random.Shared;

    public NapalmVulcanStrike(Random? rng = null) : base(SkillIds.HN_NAPALM_VULCAN_STRIKE)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 + 650 * skillLevel) + 3 * src.Stats.Spl;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
