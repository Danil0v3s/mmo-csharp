using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_SAVAGE_IMPACT — Savage Impact. Manual port of
/// <c>rathena-fork/src/map/skills/thief/savageimpact.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 105*lv) + 5*pow</c>;
/// <c>+20*lv + 2*pow</c> under SC_SHADOW_EXCEED. Before the splash
/// the caster slides into a cell adjacent to the target; under
/// SC_CLOAKINGEXCEED the cloak ends.
/// </summary>
public sealed class SavageImpact : RecursiveDamageSplashSkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public SavageImpact() : base(SkillIds.SHC_SAVAGE_IMPACT) { }

    public SavageImpact(IUnitOpsService? unitOps = null) : base(SkillIds.SHC_SAVAGE_IMPACT)
    {
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 105 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.ShadowExceed) != null)
        {
            ratio += 20 * skillLevel;
            ratio += 2 * src.Stats.Pow;
        }
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena splashSearch: if SC_CLOAKINGEXCEED on caster, end it.
        ctx.Sc?.End(src, StatusType.Cloakingexceed);
        _unitOps?.CheckUnitMovePos(src, target.X, target.Y, easy: 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
