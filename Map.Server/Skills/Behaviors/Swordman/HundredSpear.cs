using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_HUNDREDSPEAR — Rune Knight Hundred Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/hundredspear.cpp</c>.
/// Ratio <c>+(-100 + 600 + 200*lv) + 50*SpiralPierce_lv</c>; with
/// SC_DRAGONIC_AURA, <c>+160*aura_lv</c>; with SC_CHARGINGPIERCE_COUNT
/// val1 ≥ 10, the ratio doubles. SC plumbing into ratio is TODO.
/// </summary>
public sealed class HundredSpear : RecursiveDamageSplashSkillImpl
{
    public HundredSpear() : base(SkillIds.RK_HUNDREDSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 600 + 200 * skillLevel);
        // rAthena: + 50 * pc_checkskill(sd, LK_SPIRALPIERCE).
        if (src is PlayerEntity sd)
        {
            ratio += 50 * sd.LearnedSkills.GetValueOrDefault(SkillIds.LK_SPIRALPIERCE);
        }
        // DragonicAura / ChargingPierceCount SC bonuses are read from
        // the caster's status table. The status_change Val storage
        // isn't exposed on this hook (no ctx), so the read lands when
        // the ratio path threads ctx through.
        return ratio;
    }
}
