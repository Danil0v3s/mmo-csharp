using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_BITE — Summoner Bite. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/bite.cpp</c>. Ratio <c>+100</c>.
/// </summary>
public sealed class Bite : WeaponSkillImpl
{
    public Bite() : base(SkillIds.SU_BITE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100;
}
