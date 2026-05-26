using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_FATALMENACE — Fatal Menace. Manual port of
/// <c>rathena-fork/src/map/skills/thief/fatalmenace.cpp</c>.
/// Splash damage; ratio <c>+(120*lv + Agi)</c>, <c>+30*lv</c> when
/// caster has SC_ABYSS_DAGGER. Dagger casters land an extra hit
/// (rAthena <c>dmg.div_++</c>). The original rAthena body emits a
/// chip-damage frame on the caster (the recall-target visual); we
/// elide the visual but the splash + hit-rate curve are full parity.
/// </summary>
public sealed class FatalMenace : WeaponSkillImpl
{
    /// <summary>rAthena <c>W_DAGGER</c> — caster's WeaponType that
    /// triggers the +1 div_ branch.</summary>
    private const int W_DAGGER = 1;

    public FatalMenace() : base(SkillIds.SC_FATALMENACE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + 120 * skillLevel + src.Stats.Agi;
        if (ctx.Sc?.Get(src, StatusType.AbyssDagger) != null)
            ratio += 30 * skillLevel;
        return ratio;
    }

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: if (sd.weapontype1 == W_DAGGER) dmg.div_++.
        if (src is PlayerEntity pc && pc.WeaponType == W_DAGGER)
            dmg.Hits = dmg.Hits + 1;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: lv < 6 → hit -= 35 - 5*lv; lv > 6 → hit += 5*lv - 30.
        if (skillLevel < 6)
            return (short)(hitRate - (35 - 5 * skillLevel));
        if (skillLevel > 6)
            return (short)(hitRate + (5 * skillLevel - 30));
        return hitRate;
    }
}
