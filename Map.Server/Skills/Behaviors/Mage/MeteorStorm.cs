using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_METEOR — Wizard Meteor Storm. Manual port of
/// <c>rathena-fork/src/map/skills/mage/meteorstorm.cpp</c>.
///
/// <para>Drops a sequence of Meteor ground units inside a 3×3 splash
/// around the cast XY, staggered by the unit interval. Renewal: +25 %
/// MATK. Hit victims roll <c>3*lv %</c> SC_STUN. The per-meteor
/// staggered placement requires the unit-interval read which isn't on
/// our ISkillUnitService — we drop a single unit centered on the cast
/// for now and leave the stagger as TODO.</para>
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
        if (_rng.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
    }
}
