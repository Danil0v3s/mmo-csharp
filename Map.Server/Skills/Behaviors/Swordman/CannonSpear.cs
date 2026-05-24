using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_CANNONSPEAR — Royal Guard Cannon Spear (skill.cpp:LG_CANNONSPEAR).
/// Ratio <c>baseRatio + (-100 + lv*(120 + STR))</c>; +400 when the
/// caster has <c>SC_SPEAR_SCAR</c> active.
/// </summary>
public sealed class CannonSpear : RecursiveDamageSplashSkillImpl
{
    public CannonSpear() : base(SkillIds.LG_CANNONSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + skillLevel * (120 + src.Stats.Str));
        if (ctx.Sc?.Get(src, Map.Server.Status.StatusType.SpearScar) != null) ratio += 400;
        return ratio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
