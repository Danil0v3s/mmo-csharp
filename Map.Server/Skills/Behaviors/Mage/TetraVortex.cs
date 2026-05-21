using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_TETRAVORTEX — Warlock Tetra Vortex. Manual port of
/// <c>rathena-fork/src/map/skills/mage/tetravortex.cpp</c>.
///
/// <para>Detonates 4 elemental sub-hits staggered 200 ms apart
/// (Fire/Wind/Water/Ground). Mob caster cycles a fixed order. Player
/// caster reads SC_SPHERE_1..5 and fires the elements actually
/// summoned. Sub-skill ids (WL_TETRAVORTEX_*) and SC_SPHERE_* slot
/// machinery aren't on our catalog yet, so we schedule four
/// follow-up Tetra Vortex hits via the timer and treat them as a
/// single uniform magic chain.</para>
/// </summary>
public sealed class TetraVortex : SkillImpl
{
    private readonly ISkillTimerService? _timers;
    private readonly ISkillAttackService? _skillAttack;

    public TetraVortex() : base(SkillIds.WL_TETRAVORTEX) { }

    public TetraVortex(
        ISkillTimerService? timers = null,
        ISkillAttackService? skillAttack = null) : base(SkillIds.WL_TETRAVORTEX)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // 4 hits 200 ms apart. Per-element sub-skill dispatch TODO.
        for (var i = 0; i < 4; i++)
        {
            _timers?.Schedule(src, target, i * 200, SkillId, skillLevel,
                (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv));
        }
    }
}
