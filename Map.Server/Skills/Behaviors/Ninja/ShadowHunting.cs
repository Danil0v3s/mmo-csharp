using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KAGEGARI — Shadow Hunting. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowhunting.cpp</c>.
/// POS2 splash; ratio <c>+(-100 + 600 + 900*lv) + 5*pow</c>.
/// SS_KAGEGISSEN partner bonus is TODO.
/// </summary>
public sealed class ShadowHunting : SkillImpl
{
    public ShadowHunting() : base(SkillIds.SS_KAGEGARI) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 600 + 900 * skillLevel) + 5 * src.Stats.Pow;
}
