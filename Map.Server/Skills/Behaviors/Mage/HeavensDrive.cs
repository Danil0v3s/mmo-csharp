using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_HEAVENDRIVE — Wizard Heaven's Drive. Manual port of
/// <c>rathena-fork/src/map/skills/mage/heavensdrive.cpp</c>.
///
/// <para>Drops the Heaven's Drive ground unit at the cast XY (Earth
/// damage). Renewal: +25 % MATK on the ratio. Cancels SC_SV_ROOTTWIST
/// on hit victims.</para>
/// </summary>
public sealed class HeavensDrive : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public HeavensDrive() : base(SkillIds.WZ_HEAVENDRIVE) { }

    public HeavensDrive(ISkillUnitService? units = null) : base(SkillIds.WZ_HEAVENDRIVE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: +25.
        return baseRatio + 25;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.SvRoottwist);
    }
}
