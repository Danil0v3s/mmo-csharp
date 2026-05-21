using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// PA_SACRIFICE — Paladin Martyr's Reckoning / Sacrifice. Manual port
/// of <c>rathena-fork/src/map/skills/swordman/martyrsreckoning.cpp</c>.
/// Ratio <c>+(-10 + 10*lv)</c>. CastendNoDamageId applies SC_SACRIFICE.
/// </summary>
public sealed class MartyrsReckoning : WeaponSkillImpl
{
    public MartyrsReckoning() : base(SkillIds.PA_SACRIFICE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-10 + 10 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Sacrifice, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
