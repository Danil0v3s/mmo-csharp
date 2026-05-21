using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_ETERNAL_SLASH — Eternal Slash. Manual port of
/// <c>rathena-fork/src/map/skills/thief/eternalslash.cpp</c>.
/// Ratio <c>+(-100 + 300*lv) + 2*pow</c>; +120*lv +pow under
/// SC_SHADOW_EXCEED. Hit count comes from SC_E_SLASH_COUNT (TODO).
/// </summary>
public sealed class EternalSlash : WeaponSkillImpl
{
    public EternalSlash() : base(SkillIds.SHC_ETERNAL_SLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 * skillLevel) + 2 * src.Stats.Pow;
}
