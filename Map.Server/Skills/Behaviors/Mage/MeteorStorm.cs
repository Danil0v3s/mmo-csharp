using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_METEOR — Wizard Meteor Storm. Manual port of
/// <c>rathena-fork/src/map/skills/mage/meteorstorm.cpp</c>.
///
/// <para>Drops <c>2 + lv</c> Meteor ground units at random offsets
/// inside a 9×9 envelope around the cast XY. Renewal: +25 % MATK.
/// Hit victims roll <c>3*lv %</c> SC_STUN.</para>
///
/// <para>INFRA-DEFERRED: rAthena staggers each meteor via
/// <c>skill_addtimerskill</c> with a position-targeted timer; our
/// <see cref="ISkillTimerService"/> only schedules entity-targeted
/// callbacks. We drop all meteors in the same tick — the per-unit
/// damage interval still applies via <c>skill_unit_db.Interval</c>.</para>
/// </summary>
public sealed class MeteorStorm : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public MeteorStorm() : base(SkillIds.WZ_METEOR) => _rng = Random.Shared;

    public MeteorStorm(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.WZ_METEOR)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 25;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena meteor pattern: spawn N meteors at random offsets inside
        // a 9×9 envelope centered on (x,y). Count = 2 + skill_lv (skill.cpp
        // WZ_METEOR arm). Renewal staggers via skill_addtimerskill; without
        // a unit_interval reader we drop them all in the same tick (the
        // skill_unit_db Interval applies to per-unit damage ticks anyway).
        const short Half = 4;
        var count = 2 + skillLevel;
        for (var i = 0; i < count; i++)
        {
            var ox = (short)(x + _rng.Next(-Half, Half + 1));
            var oy = (short)(y + _rng.Next(-Half, Half + 1));
            _units?.Place(src, SkillId, skillLevel, ox, oy);
        }
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // skill.cpp WZ_METEOR arm: 3*lv% stun → rate 3*lv*100 (1/100-% units).
        // SKILL-01: pass the raw rate; the SC engine runs status_get_sc_def
        // (VIT resist + level-diff + boss immunity) and rolls. (_rng stays —
        // it still places the meteor cells in CastendPos2.)
        ctx.Sc?.Start(target, StatusType.Stun, rate: 3 * skillLevel * 100,
            val1: skillLevel, 0, 0, 0, durationMs: 3000, source: src);
    }
}
