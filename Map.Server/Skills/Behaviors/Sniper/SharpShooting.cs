using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Sniper;

/// <summary>
/// SN_SHARPSHOOTING — Sniper Sharp Shooting. Mirrors
/// <c>rathena-fork/src/map/skills/sniper/sharpshooting.cpp</c>.
///
/// Long-range single-target physical at (200 + 50*lv)% ATK with
/// guaranteed critical hit (rAthena: cri_chance = 1000). The
/// guaranteed crit ports separately when BattleCalculator gains
/// a per-skill critical-override flag.
/// </summary>
public sealed class SharpShooting : WeaponSkillImpl
{
    public SharpShooting() : base(SkillIds.SN_SHARPSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 + 50 * skillLevel; // base 100 + this = 200 + 50*lv
}
