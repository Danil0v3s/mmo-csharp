using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_BREAKER — Soul Destroyer. Manual port of
/// <c>rathena-fork/src/map/skills/thief/souldestroyer.cpp</c>.
/// Renewal: <c>+(-100 + 150*lv + Str + Int)</c>.
/// </summary>
public sealed class SoulDestroyer : WeaponSkillImpl
{
    public SoulDestroyer() : base(SkillIds.ASC_BREAKER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 150 * skillLevel) + src.Stats.Str + src.Stats.IntStat;
}
