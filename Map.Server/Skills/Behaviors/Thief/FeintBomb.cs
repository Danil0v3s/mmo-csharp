using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_FEINTBOMB — Feint Bomb. Manual port of
/// <c>rathena-fork/src/map/skills/thief/feintbomb.cpp</c>.
/// Drops a feint-bomb ground unit at (x, y), slides the caster
/// backwards (BLOWN_IGNORE_NO_KNOCKBACK) and latches SC_FEINTBOMB on
/// the caster — the SC turns the caster into a decoy so subsequent
/// mob retargeting goes to the unit, not the player.
/// </summary>
public sealed class FeintBomb : WeaponSkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly IUnitOpsService? _unitOps;

    /// <summary>rAthena <c>skill_get_blewcount(SC_FEINTBOMB, lv)</c> —
    /// 3-cell self-knockback.</summary>
    private const int BLEW_COUNT = 3;

    public FeintBomb() : base(SkillIds.SC_FEINTBOMB) { }

    public FeintBomb(ISkillUnitService? units = null, IUnitOpsService? unitOps = null)
        : base(SkillIds.SC_FEINTBOMB)
    {
        _units = units;
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var jobLv = src is PlayerEntity p ? p.JobLevel : 50;
        return baseRatio + (-100 + (skillLevel + 1) * src.Stats.Dex / 2 * (jobLv / 10));
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Drop the bomb ground unit (rAthena returns the group pointer;
        // a null group fails the cast with NOCONSUME_REQ).
        _units?.Place(src, SkillId, skillLevel, x, y);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);

        // Self-knockback in facing direction.
        var dir = _unitOps?.GetDir(src) ?? 0;
        _unitOps?.BlownBy(src, dir, BLEW_COUNT);

        // SC_FEINTBOMB on caster — turns mobs onto the bomb.
        ctx.Sc?.Start(src, StatusType.Feintbomb, val1: skillLevel, 0, 0, 0,
            durationMs: 5_000, src);
    }
}
