using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_DOUBLEBOWLINGBASH — Hyper Novice Double Bowling Bash. Manual port
/// of <c>rathena-fork/src/map/skills/novice/doublebowlingbash.cpp</c>.
/// Ratio <c>+(-100 + 250 + 400*lv) + 5*POW</c>. On cast applies
/// SC_HNNOWEAPON to the caster. Miscflag-based hit count and
/// HN_SELFSTUDY_TATICS bonus are TODO.
/// </summary>
public sealed class DoubleBowlingBash : SkillImpl
{
    public DoubleBowlingBash() : base(SkillIds.HN_DOUBLEBOWLINGBASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 400 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.Hnnoweapon, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
