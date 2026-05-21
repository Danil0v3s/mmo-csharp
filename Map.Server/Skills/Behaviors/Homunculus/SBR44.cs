using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HFLI_SBR44 — Filir SBR44. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_sbr44.cpp</c>.
/// Ratio <c>+100*(lv-1)</c>. Drops homunculus intimacy on cast (TODO).
/// </summary>
public sealed class SBR44 : WeaponSkillImpl
{
    public SBR44() : base(SkillIds.HFLI_SBR44) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
