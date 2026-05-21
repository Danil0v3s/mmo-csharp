using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_FATALMENACE — Fatal Menace. Manual port of
/// <c>rathena-fork/src/map/skills/thief/fatalmenace.cpp</c>.
/// Splash damage; ratio <c>+(120*lv + Agi)</c>, +30*lv when source
/// has SC_ABYSS_DAGGER. Dagger-bonus div_ + warp on hit are TODO.
/// </summary>
public sealed class FatalMenace : WeaponSkillImpl
{
    public FatalMenace() : base(SkillIds.SC_FATALMENACE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 120 * skillLevel + src.Stats.Agi;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => skillLevel < 6 ? (short)(hitRate - 50) : hitRate;
}
