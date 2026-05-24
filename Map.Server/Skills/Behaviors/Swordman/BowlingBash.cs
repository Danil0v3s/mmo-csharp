using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_BOWLINGBASH — Knight Bowling Bash (skill.cpp:KN_BOWLINGBASH).
/// Renewal: ratio <c>baseRatio + 40*lv</c>. Two-handed sword wielders
/// (<c>W_2HSWORD = 3</c>) get a 2-hit attack (rAthena: damage.div_ = 2
/// when the caster has a 2H sword). The pre-renewal "gutter" splash
/// chain isn't ported.
/// </summary>
public sealed class BowlingBash : WeaponSkillImpl
{
    /// <summary>rAthena <c>W_2HSWORD</c> — caster's WeaponType when a
    /// two-handed sword is equipped. Drives the 2-hit damage branch.</summary>
    private const int W_2HSWORD = 3;

    public BowlingBash() : base(SkillIds.KN_BOWLINGBASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 40 * skillLevel;

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        if (src is PlayerEntity pc && pc.WeaponType == W_2HSWORD)
        {
            dmg.Hits = 2;
        }
    }
}
