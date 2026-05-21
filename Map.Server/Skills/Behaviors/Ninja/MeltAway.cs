using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_TOKEDASU — Melt Away. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/meltaway.cpp</c>.
/// POS2 area + caster knockback away from target. Ratio
/// <c>+(-100 + 700*lv) + 5*con</c>. Self-SC + blown self are TODO.
/// </summary>
public sealed class MeltAway : SkillImpl
{
    public MeltAway() : base(SkillIds.SS_TOKEDASU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 700 * skillLevel) + 5 * src.Stats.Con;
}
