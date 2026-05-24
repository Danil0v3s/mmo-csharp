using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_CHARGEATK — Knight Charge Attack (skill.cpp:KN_CHARGEATK arm).
/// Renewal-pre fixed ratio bump <c>+600</c>; the cast slides the caster
/// adjacent to the target (after a <c>path_search_long</c> wall check),
/// lands a single hit, and pushes the target back by skill_db.Blewcount.
/// </summary>
public sealed class ChargeAttack : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ChargeAttack() : base(SkillIds.KN_CHARGEATK) { }

    public ChargeAttack(ISkillAttackService? skillAttack = null) : base(SkillIds.KN_CHARGEATK)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 600;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // path_search_long(src, target.x, target.y, CELL_CHKWALL) gate:
        // if a wall blocks the line, the slide+hit is refused (rAthena
        // returns from skill_castend_damage_id without doing anything).
        // When the path service isn't wired (tests), permit by default.
        var pathClear = ctx.Paths?.PathSearchLong(src.MapId, src.X, src.Y, target.X, target.Y) ?? true;
        if (!pathClear) return;

        // Slide adjacent to target; rAthena uses skill_check_unit_movepos
        // which our ctx.UnitOps exposes verbatim.
        ctx.UnitOps?.CheckUnitMovePos(src, target.X, target.Y, 1);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

        // After the strike, push the target one cell away from the caster.
        // rAthena: skill_blown(src, target, skill_get_blewcount, dir, 0)
        // where dir = -1 auto-resolves to map_calc_dir(target, src.x, src.y);
        // we approximate by stepping the target along (src → target) vector.
        var dx = Math.Sign(target.X - src.X);
        var dy = Math.Sign(target.Y - src.Y);
        var dir = DirectionFromDelta(dx, dy);
        if (dir >= 0)
        {
            ctx.UnitOps?.BlownBy(target, dir, count: 1);
        }
    }

    private static int DirectionFromDelta(int dx, int dy) => (dx, dy) switch
    {
        (0, 1)   => 0,
        (1, 1)   => 1,
        (1, 0)   => 2,
        (1, -1)  => 3,
        (0, -1)  => 4,
        (-1, -1) => 5,
        (-1, 0)  => 6,
        (-1, 1)  => 7,
        _        => -1,
    };
}
