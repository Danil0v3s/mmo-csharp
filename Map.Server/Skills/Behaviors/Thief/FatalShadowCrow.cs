using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_FATAL_SHADOW_CROW — Fatal Shadow Crow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/fatalshadowcrow.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 1300*lv + 10*pow)</c>;
/// <c>+150*lv</c> vs Demihuman / Dragon. Applies SC_DARKCROW on hit
/// (the SC level mirrors the caster's learned GC_DARKCROW level —
/// minimum 1). Before the splash launches the caster slides one
/// cell between itself and the target.
/// </summary>
public sealed class FatalShadowCrow : RecursiveDamageSplashSkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public FatalShadowCrow() : base(SkillIds.SHC_FATAL_SHADOW_CROW) { }

    public FatalShadowCrow(IUnitOpsService? unitOps = null) : base(SkillIds.SHC_FATAL_SHADOW_CROW)
    {
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 1300 * skillLevel) + 10 * src.Stats.Pow;
        if (target.Stats.Race == BattleRace.Demihuman || target.Stats.Race == BattleRace.Dragon)
            ratio += 150 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena splashSearch: slide caster one cell between src
        // and target (skill_check_unit_movepos easy=0 checkColl=1).
        _unitOps?.CheckUnitMovePos(src, target.X, target.Y, easy: 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start(src, target, SC_DARKCROW, 100,
        //   max(1, pc_checkskill(sd, GC_DARKCROW)),
        //   skill_get_time(getSkillId(), skill_lv)).
        var crow = src is PlayerEntity pc
            ? System.Math.Max(1, ctx.PlayerSkill?.CheckSkill(pc, SkillIds.GC_DARKCROW) ?? 0)
            : skillLevel;
        ctx.Sc?.Start(target, StatusType.Darkcrow, val1: crow, 0, 0, 0,
            durationMs: 5_000 + 5_000 * crow, src);
    }
}
