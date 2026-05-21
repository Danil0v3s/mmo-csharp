using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_HYOUSYOURAKU — Ice Meteor. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/icemeteor.cpp</c>.
/// +50*lv ratio + +100*charms when wielding water charms. POS2
/// unit-set + SC_FREEZE (10+10*lv)% follow-up.
/// </summary>
public sealed class IceMeteor : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public IceMeteor() : base(SkillIds.NJ_HYOUSYOURAKU) { }

    public IceMeteor(ISkillUnitService? units = null) : base(SkillIds.NJ_HYOUSYOURAKU)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 50 * skillLevel;
        // TODO: +100 * spiritcharm when CHARM_TYPE_WATER.
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 10 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
