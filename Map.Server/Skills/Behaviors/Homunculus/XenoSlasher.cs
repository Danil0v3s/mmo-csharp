using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_XENO_SLASHER — Homunculus Xeno Slasher. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_xenoslasher.cpp</c>.
/// Ratio <c>+(-100 + 450*lv*BaseLv/100) + INT</c>. On hit applies
/// SC_BLEEDING.
/// </summary>
public sealed class XenoSlasher : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public XenoSlasher() : base(SkillIds.MH_XENO_SLASHER) { }

    public XenoSlasher(ISkillUnitService? units = null) : base(SkillIds.MH_XENO_SLASHER)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 * skillLevel * src.Level / 100) + src.Stats.IntStat;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
