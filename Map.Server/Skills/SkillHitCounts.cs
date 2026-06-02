using System;
using System.Collections.Generic;

namespace Map.Server.Skills;

/// <summary>
/// COMBAT-39 — rAthena <c>skill_db</c> <c>HitCount</c> (the <c>num</c> column) for
/// every multi-hit weapon skill, transcribed verbatim from
/// <c>db/re/skill_db.yml</c>. Sign is preserved exactly: a <b>negative</b> count
/// is "one damage value displayed as |N| hits" (the wire shows |N|; the ratio
/// carries the full total), a <b>positive</b> count is "per-hit damage ×N". The
/// magnitude drives <c>ZC_NOTIFY_ACT3.div</c> (via
/// <see cref="Behaviors.WeaponSkillImpl.GetMultiHitCount"/>); the sign is consumed
/// by the positive-div per-hit damage multiply (COMBAT-60).
///
/// <para>This is the interim single-source for hit counts until the skill_db
/// <c>HitCount</c> column is surfaced through <see cref="SkillDbEntity"/> /
/// <see cref="SkillDbLoader"/> (the data-column route — a possible follow-up).</para>
/// </summary>
public static class SkillHitCounts
{
    // Each entry is the signed per-level count. A length-1 array = constant across
    // all levels; a longer array is indexed by (level − 1), clamped.
    private static readonly Dictionary<ushort, int[]> _table = new()
    {
        // ---- constant across levels ----
        { SkillIds.TF_DOUBLE, new[] { 2 } },
        { SkillIds.AS_SONICBLOW, new[] { -8 } },
        { SkillIds.MO_TRIPLEATTACK, new[] { -3 } },
        { SkillIds.MO_CHAINCOMBO, new[] { -4 } },
        { SkillIds.MO_FINGEROFFENSIVE, new[] { -5 } },
        { SkillIds.KN_PIERCE, new[] { 3 } },
        { SkillIds.KN_BOWLINGBASH, new[] { 2 } },
        { SkillIds.KN_BRANDISHSPEAR, new[] { -3 } },
        { SkillIds.CR_HOLYCROSS, new[] { -2 } },
        { SkillIds.PA_SHIELDCHAIN, new[] { 5 } },
        { SkillIds.LK_SPIRALPIERCE, new[] { 5 } },
        { SkillIds.ABC_FRENZY_SHOT, new[] { 2 } },
        { SkillIds.BA_MUSICALSTRIKE, new[] { 2 } },
        { SkillIds.CD_EFFLIGO, new[] { -7 } },
        { SkillIds.CG_ARROWVULCAN, new[] { -9 } },
        { SkillIds.DC_THROWARROW, new[] { 2 } },
        { SkillIds.GC_CROSSIMPACT, new[] { -7 } },
        { SkillIds.GC_DARKCROW, new[] { 3 } },
        { SkillIds.GS_CHAINACTION, new[] { 2 } },
        { SkillIds.GS_RAPIDSHOWER, new[] { -5 } },
        { SkillIds.GS_TRIPLEACTION, new[] { 3 } },
        { SkillIds.HN_MEGA_SONIC_BLOW, new[] { -8 } },
        { SkillIds.HN_SHIELD_CHAIN_RUSH, new[] { -5 } },
        { SkillIds.HN_SPIRAL_PIERCE_MAX, new[] { -5 } },
        { SkillIds.IG_IMPERIAL_CROSS, new[] { 3 } },
        { SkillIds.KO_JYUMONJIKIRI, new[] { -2 } },
        { SkillIds.LG_HESPERUSLIT, new[] { 3 } },
        { SkillIds.LG_SHIELDPRESS, new[] { -5 } },
        { SkillIds.MA_DOUBLE, new[] { 2 } },
        { SkillIds.ML_PIERCE, new[] { 3 } },
        { SkillIds.ML_SPIRALPIERCE, new[] { 5 } },
        { SkillIds.MT_TRIPLE_LASER, new[] { 3 } },
        { SkillIds.NJ_KASUMIKIRI, new[] { -2 } },
        { SkillIds.NJ_KIRIKAGE, new[] { -3 } },
        { SkillIds.NJ_KUNAI, new[] { 3 } },
        { SkillIds.NPC_DARKCROSS, new[] { -2 } },
        { SkillIds.NW_BASIC_GRENADE, new[] { -2 } },
        { SkillIds.NW_HASTY_FIRE_IN_THE_HOLE, new[] { -2 } },
        { SkillIds.NW_MAGAZINE_FOR_ONE, new[] { 6 } },
        { SkillIds.NW_MISSION_BOMBARD, new[] { -3 } },
        { SkillIds.NW_WILD_FIRE, new[] { -3 } },
        { SkillIds.RA_AIMEDBOLT, new[] { 5 } },
        { SkillIds.RK_SONICWAVE, new[] { 3 } },
        { SkillIds.SC_TRIANGLESHOT, new[] { -3 } },
        { SkillIds.SHC_SHADOW_STAB, new[] { 3 } },
        { SkillIds.SH_CHUL_HO_SONIC_CLAW, new[] { -2 } },
        { SkillIds.SR_DRAGONCOMBO, new[] { -2 } },
        { SkillIds.SR_FALLENEMPIRE, new[] { -2 } },
        { SkillIds.SR_GATEOFHELL, new[] { -7 } },
        { SkillIds.SS_FUUMAKOUCHIKU, new[] { -3 } },
        { SkillIds.SU_PICKYPECK, new[] { -5 } },
        { SkillIds.TK_COUNTER, new[] { -3 } },
        { SkillIds.TK_DOWNKICK, new[] { -3 } },
        { SkillIds.TR_RHYTHMSHOOTING, new[] { 3 } },
        { SkillIds.TR_ROSEBLOSSOM, new[] { -2 } },
        { SkillIds.WH_HAWKRUSH, new[] { -2 } },
        { SkillIds.WH_WILD_WALK, new[] { -3 } },

        // ---- per-level ----
        { SkillIds.CH_CHAINCRUSH, new[] { -1, -1, -2, -2, -3, -3, -4, -4, -5, -5 } },
        { SkillIds.DK_STORMSLASH, new[] { 1, 2, 3, 4, 5 } },
        { SkillIds.HFLI_MOON, new[] { -1, -2, -2, -2, -3 } },
        { SkillIds.NPC_COMBOATTACK, new[] { -2, -3, -4, -5, -6, -7, -8, -9, -10, -11 } },
        { SkillIds.CR_ACIDDEMONSTRATION, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } },
    };

    /// <summary>
    /// Signed rAthena hit count for <paramref name="skillId"/> at
    /// <paramref name="skillLevel"/> (1-based); 1 (single hit) when the skill has
    /// no <c>HitCount</c> row.
    /// </summary>
    public static int Get(ushort skillId, ushort skillLevel)
    {
        if (!_table.TryGetValue(skillId, out var arr) || arr.Length == 0) return 1;
        var idx = Math.Clamp(skillLevel - 1, 0, arr.Length - 1);
        return arr[idx];
    }
}
