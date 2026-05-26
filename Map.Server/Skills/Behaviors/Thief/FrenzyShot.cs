using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_FRENZY_SHOT — Frenzy Shot. Manual port of
/// <c>rathena-fork/src/map/skills/thief/frenzyshot.cpp</c>.
/// Ratio <c>+(-100 + 250 + 800*lv) + 15*con</c>. Triple-hit roll at
/// <c>5*lv</c>% inflates <c>dmg.div_ = 3</c>; otherwise the default
/// 1-hit applies.
/// </summary>
public sealed class FrenzyShot : WeaponSkillImpl
{
    public FrenzyShot() : base(SkillIds.ABC_FRENZY_SHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 800 * skillLevel) + 15 * src.Stats.Con;

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: rnd_chance(5 * skill_lv, 100) → dmg.div_ = 3.
        if (System.Random.Shared.Next(100) < 5 * skillLevel)
            dmg.Hits = 3;
    }
}
