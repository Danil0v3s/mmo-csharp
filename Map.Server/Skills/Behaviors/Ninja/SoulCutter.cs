using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_SETSUDAN — Soul Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/soulcutter.cpp</c>.
/// Ratio <c>+100*(lv-1)</c>. +200*linkLv extra when the target has any
/// of SC_SPIRIT / SC_SOULGOLEM / SC_SOULSHADOW / SC_SOULFALCON /
/// SC_SOULFAIRY; ends those SCs on hit (TODO).
/// </summary>
public sealed class SoulCutter : WeaponSkillImpl
{
    public SoulCutter() : base(SkillIds.KO_SETSUDAN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
