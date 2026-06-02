using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// COMBAT-55 — Ranger trap misc-damage formula (rAthena <c>battle_calc_misc_attack</c>,
/// battle.cpp:9762 for RA_CLUSTERBOMB / RA_FIRINGTRAP / RA_ICEBOUNDTRAP):
/// <list type="number">
///   <item>base = <c>skill_lv * DEX + INT * 5</c>;</item>
///   <item><c>RE_LVL_TMDMOD()</c> — above base level 99:
///         <c>dmg = dmg*150/100 + dmg*level/100</c> (const.hpp:102);</item>
///   <item>Research Trap multiplier: a PLAYER caster deals
///         <c>dmg * 20 * researchLv / (CLUSTERBOMB ? 50 : 100)</c>, or <b>0</b> with no
///         RA_RESEARCHTRAP learned; a non-player caster deals
///         <c>dmg * 200 / (CLUSTERBOMB ? 50 : 100)</c>.</item>
/// </list>
/// The traps set NK_IGNOREELEMENT/FLEE/DEFCARD — applied as raw damage (no def reduce).
/// </summary>
public static class TrapDamage
{
    public static long Compute(ushort skillId, ushort skillLevel, Entity caster)
    {
        long dmg = (long)skillLevel * caster.Stats.Dex + caster.Stats.IntStat * 5;

        // RE_LVL_TMDMOD — Ranger-trap level scaling above 99.
        if (caster.Level > 99)
            dmg = dmg * 150 / 100 + dmg * caster.Level / 100;

        int divisor = skillId == SkillIds.RA_CLUSTERBOMB ? 50 : 100;
        if (caster is PlayerEntity pc)
        {
            int research = pc.LearnedSkills.GetValueOrDefault(SkillIds.RA_RESEARCHTRAP);
            dmg = research > 0 ? dmg * 20 * research / divisor : 0;
        }
        else
        {
            dmg = dmg * 200 / divisor;
        }
        return dmg < 0 ? 0 : dmg;
    }
}
