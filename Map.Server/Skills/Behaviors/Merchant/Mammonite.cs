using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_MAMMONITE — Merchant Mammonite. Mirrors
/// <c>rathena-fork/src/map/skills/merchant/mammonite.cpp</c>.
///
/// Single physical hit at (100 + 50*lv)% ATK. Pays 100*lv zeny on
/// top of SP — zeny cost lives in skill_db.ZenyCost (upstream) so
/// the plugin just runs the damage formula.
/// </summary>
public sealed class Mammonite : WeaponSkillImpl
{
    public Mammonite() : base(SkillIds.MC_MAMMONITE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
