using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_CANNONSPEAR — Royal Guard Cannon Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/cannonspear.cpp</c>.
/// Ratio <c>+(-100 + lv*(120 + STR))</c>; +400 with SC_SPEAR_SCAR.
/// SpearScar check is TODO (needs ctx.Sc plumbing into ratio calc).
/// </summary>
public sealed class CannonSpear : RecursiveDamageSplashSkillImpl
{
    public CannonSpear() : base(SkillIds.LG_CANNONSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + skillLevel * (120 + src.Stats.Str));
    // Deferred: rAthena adds +400 when src has SC_SPEAR_SCAR.
    // CalculateSkillRatio has no SkillBehaviorContext parameter, so the SC
    // table is not reachable here; needs a ratio-hook signature change.

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
