namespace Map.Server.Skills;

/// <summary>
/// COMBAT-56 — the set of skills whose rAthena ratio arm OMITS the renewal
/// level-damage macro, so they must NOT receive the above-level-99 scaling the
/// C# port otherwise applies blanket (weapon: <c>SkillImpl.ReLvlDivisor</c>;
/// magic/misc: the unconditional <c>×lv/100</c> in <c>BattleCalculator</c>).
///
/// <para><see cref="RatioOmit"/> — arms of <c>battle_calc_attack_skill_ratio</c>
/// (weapon + magic) that omit <c>RE_LVL_DMOD</c>/<c>RE_LVL_MDMOD</c>.
/// <see cref="MiscOmit"/> — arms of <c>battle_calc_misc_attack</c> that omit
/// <c>RE_LVL_MDMOD</c>.</para>
///
/// <para>rAthena gates the macro per-arm (no <c>INF2_DISABLELVDMG</c> flag exists
/// in this checkout), so the disable is encoded here as the internal "this arm
/// omits the macro" marker the ticket calls for. Generated from a scan of
/// <c>src/map/battle.cpp</c> (resolved against <see cref="SkillIds"/>).</para>
/// </summary>
public static class ReLvlDmodOmit
{
    private static readonly System.Collections.Generic.HashSet<ushort> RatioOmit = new()
    {
        SkillIds.ABR_BATTLE_BUSTER, SkillIds.ABR_DUAL_CANNON_FIRE, SkillIds.ABR_INFINITY_BUSTER,
        SkillIds.AC_CHARGEARROW, SkillIds.AC_DOUBLE, SkillIds.AC_SHOWER, SkillIds.AM_ACIDTERROR,
        SkillIds.AM_DEMONSTRATION, SkillIds.AS_GRIMTOOTH, SkillIds.AS_POISONREACT, SkillIds.AS_SONICBLOW,
        SkillIds.AS_SPLASHER, SkillIds.AS_VENOMKNIFE, SkillIds.BA_MUSICALSTRIKE,
        SkillIds.CR_ACIDDEMONSTRATION, SkillIds.CR_HOLYCROSS, SkillIds.CR_SHIELDBOOMERANG,
        SkillIds.CR_SHIELDCHARGE, SkillIds.DC_THROWARROW, SkillIds.EL_CIRCLE_OF_FIRE,
        SkillIds.EL_FIRE_BOMB_ATK, SkillIds.EL_FIRE_WAVE_ATK, SkillIds.EL_HURRICANE,
        SkillIds.EL_ROCK_CRUSHER, SkillIds.EL_STONE_HAMMER, SkillIds.EL_STONE_RAIN, SkillIds.EL_TIDAL_WEAPON,
        SkillIds.EL_TYPOON_MIS, SkillIds.EL_WATER_SCREW_ATK, SkillIds.EL_WIND_SLASH, SkillIds.GC_DARKCROW,
        SkillIds.GC_PHANTOMMENACE, SkillIds.GC_VENOMPRESSURE, SkillIds.GN_CART_TORNADO,
        SkillIds.GN_SLINGITEM_RANGEMELEEATK, SkillIds.GN_WALLOFTHORN, SkillIds.GS_BULLSEYE,
        SkillIds.GS_DESPERADO, SkillIds.GS_DUST, SkillIds.GS_FULLBUSTER, SkillIds.GS_GROUNDDRIFT,
        SkillIds.GS_PIERCINGSHOT, SkillIds.GS_RAPIDSHOWER, SkillIds.GS_SPREADATTACK, SkillIds.GS_TRACKING,
        SkillIds.GS_TRIPLEACTION, SkillIds.HFLI_MOON, SkillIds.HFLI_SBR44, SkillIds.HN_SPIRAL_PIERCE_MAX,
        SkillIds.HT_PHANTASMIC, SkillIds.HT_POWER, SkillIds.KN_BOWLINGBASH, SkillIds.KN_BRANDISHSPEAR,
        SkillIds.KN_CHARGEATK, SkillIds.KN_PIERCE, SkillIds.KN_SPEARBOOMERANG, SkillIds.KN_SPEARSTAB,
        SkillIds.KO_MAKIBISHI, SkillIds.LK_HEADCRUSH, SkillIds.LK_JOINTBEAT, SkillIds.MA_CHARGEARROW,
        SkillIds.MA_DOUBLE, SkillIds.MA_SHOWER, SkillIds.MC_CARTREVOLUTION, SkillIds.MC_MAMMONITE,
        SkillIds.MER_CRASH, SkillIds.MH_BLAST_FORGE, SkillIds.MH_BLAZING_AND_FURIOUS,
        SkillIds.MH_GLANZEN_SPIES, SkillIds.MH_LAVA_SLIDE, SkillIds.MH_MAGMA_FLOW,
        SkillIds.MH_MIDNIGHT_FRENZY, SkillIds.MH_NEEDLE_OF_PARALYZE, SkillIds.MH_NEEDLE_STINGER,
        SkillIds.MH_SILVERVEIN_RUSH, SkillIds.MH_SONIC_CRAW, SkillIds.MH_STAHL_HORN,
        SkillIds.MH_THE_ONE_FIGHTER_RISES, SkillIds.MH_TOXIN_OF_MANDARA, SkillIds.ML_BRANDISH,
        SkillIds.ML_PIERCE, SkillIds.MO_BALKYOUNG, SkillIds.MO_CHAINCOMBO, SkillIds.MO_COMBOFINISH,
        SkillIds.MO_EXTREMITYFIST, SkillIds.MO_FINGEROFFENSIVE, SkillIds.MO_INVESTIGATE,
        SkillIds.MO_TRIPLEATTACK, SkillIds.MS_BASH, SkillIds.MS_BOWLINGBASH, SkillIds.MS_MAGNUM,
        SkillIds.NC_MAGMA_ERUPTION, SkillIds.NJ_HUUMA, SkillIds.NJ_KASUMIKIRI, SkillIds.NJ_KIRIKAGE,
        SkillIds.NJ_KUNAI, SkillIds.NJ_SYURIKEN, SkillIds.NJ_TATAMIGAESHI, SkillIds.NPC_ACIDBREATH,
        SkillIds.NPC_ARROWSTORM, SkillIds.NPC_BLOODDRAIN, SkillIds.NPC_COMBOATTACK, SkillIds.NPC_DARKCROSS,
        SkillIds.NPC_DARKNESSATTACK, SkillIds.NPC_DARKNESSBREATH, SkillIds.NPC_DRAGONBREATH,
        SkillIds.NPC_FIREATTACK, SkillIds.NPC_FIREBREATH, SkillIds.NPC_GROUNDATTACK,
        SkillIds.NPC_HELLJUDGEMENT, SkillIds.NPC_HELLJUDGEMENT2, SkillIds.NPC_HOLYATTACK,
        SkillIds.NPC_ICEBREATH, SkillIds.NPC_ICEBREATH2, SkillIds.NPC_IGNITIONBREAK,
        SkillIds.NPC_PIERCINGATT, SkillIds.NPC_POISONATTACK, SkillIds.NPC_PULSESTRIKE,
        SkillIds.NPC_RANDOMATTACK, SkillIds.NPC_TELEKINESISATTACK, SkillIds.NPC_THUNDERBREATH,
        SkillIds.NPC_UNDEADATTACK, SkillIds.NPC_VAMPIRE_GIFT, SkillIds.NPC_WATERATTACK,
        SkillIds.NPC_WINDATTACK, SkillIds.PA_SACRIFICE, SkillIds.RA_CLUSTERBOMB, SkillIds.RA_SENSITIVEKEEN,
        SkillIds.RA_WUGBITE, SkillIds.RA_WUGDASH, SkillIds.RA_WUGSTRIKE, SkillIds.RG_BACKSTAP,
        SkillIds.RG_INTIMIDATE, SkillIds.RG_RAID, SkillIds.RL_AM_BLAST, SkillIds.RL_FIRE_RAIN,
        SkillIds.RL_H_MINE, SkillIds.RL_MASS_SPIRAL, SkillIds.RL_SLUGSHOT, SkillIds.RL_S_STORM,
        SkillIds.SJ_NEWMOONKICK, SkillIds.SJ_PROMINENCEKICK, SkillIds.SJ_STAREMPEROR,
        SkillIds.SKE_ALL_IN_THE_SKY, SkillIds.SM_BASH, SkillIds.SM_MAGNUM, SkillIds.SN_SHARPSHOOTING,
        SkillIds.SU_BITE, SkillIds.SU_PICKYPECK, SkillIds.SU_SCAROFTAROU, SkillIds.SU_SCRATCH,
        SkillIds.SU_SVG_SPIRIT, SkillIds.TF_SPRINKLESAND, SkillIds.TK_COUNTER, SkillIds.TK_DOWNKICK,
        SkillIds.TK_JUMPKICK, SkillIds.TK_STORMKICK, SkillIds.TK_TURNKICK, SkillIds.WS_CARTTERMINATION,
    };

    private static readonly System.Collections.Generic.HashSet<ushort> MiscOmit = new()
    {
        SkillIds.GS_GROUNDDRIFT, SkillIds.HT_PHANTASMIC, SkillIds.SS_KUNAIKAITEN, SkillIds.SS_KUNAIKUSSETSU,
    };

    /// <summary>True when the skill's weapon/magic ratio arm omits RE_LVL_DMOD (no >99 scaling).</summary>
    public static bool OmitsRatioScaling(ushort skillId) => RatioOmit.Contains(skillId);

    /// <summary>True when the skill's misc arm omits RE_LVL_MDMOD (no >99 scaling).</summary>
    public static bool OmitsMiscScaling(ushort skillId) => MiscOmit.Contains(skillId);
}
