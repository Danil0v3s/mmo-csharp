using Map.Server.Entities;

namespace Map.Server.Status.StatusOps;

/// <summary>
/// Status-engine top-level operations. Canonical entry points for
/// rAthena <c>status.cpp</c> (16 047 lines, 82 public functions).
///
/// The C# port already has a working `IStatusChangeService` +
/// `IStatusCalcService` covering the SC start/end machinery and the
/// `status_calc_pc` / `status_calc_mob` recalc pipeline. This
/// service surfaces the rAthena-named operations that *aren't* on
/// those services yet (HP/SP/AP delta helpers, percent-revive,
/// status-data accessors, mode helpers, refresh_*, fix_*, …).
///
/// Methods that map 1:1 onto an existing service are documented as
/// forwarders so callers reading the rAthena port see a single
/// named entry.
/// </summary>
public interface IStatusOpsService
{
    // --- HP/SP/AP delta helpers (status_*) ----------------------------
    /// <summary>rAthena <c>status_zap</c> — apply HP/SP delta (positive = lose).</summary>
    void Zap(Entity bl, long hp, long sp);

    /// <summary>rAthena <c>status_heal</c> — apply HP/SP gain.</summary>
    int Heal(Entity bl, long hp, long sp, byte flag);

    /// <summary>rAthena <c>status_percent_heal</c>.</summary>
    int PercentHeal(Entity bl, sbyte hpPercent, sbyte spPercent);

    /// <summary>rAthena <c>status_percent_damage</c>.</summary>
    int PercentDamage(Entity src, Entity target, sbyte hpPercent, sbyte spPercent, bool can_kill);

    /// <summary>rAthena <c>status_revive</c>.</summary>
    int Revive(Entity bl, byte percentHp, byte percentSp);

    /// <summary>rAthena <c>status_fixed_revive</c>.</summary>
    int FixedRevive(Entity bl, int hp, int sp);

    /// <summary>rAthena <c>status_damage</c> — generic damage helper.</summary>
    int Damage(Entity src, Entity target, long hp, long sp, int walkDelay, byte flag);

    /// <summary>rAthena <c>status_charge</c> — consume SP / item, return success.</summary>
    bool Charge(Entity bl, long hp, long sp);

    // --- status_calc_* (recalc pipeline) ------------------------------
    /// <summary>rAthena <c>status_calc_bl</c>.</summary>
    void CalcBl(Entity bl, int flag);

    /// <summary>rAthena <c>status_calc_pc_</c>.</summary>
    void CalcPc(PlayerEntity pc, int opt);

    /// <summary>rAthena <c>status_calc_mob_</c>.</summary>
    void CalcMob(MobEntity mob, byte opt);

    /// <summary>rAthena <c>status_calc_homunculus_</c>.</summary>
    void CalcHomunculus(Entity homun, byte opt);

    /// <summary>rAthena <c>status_calc_mercenary_</c>.</summary>
    void CalcMercenary(Entity merc, byte opt);

    /// <summary>rAthena <c>status_calc_elemental_</c>.</summary>
    void CalcElemental(Entity ele, byte opt);

    /// <summary>rAthena <c>status_calc_pet_</c>.</summary>
    void CalcPet(Entity pet, byte opt);

    // --- ATK / WATK / MATK / DEF derivatives ---------------------------
    /// <summary>rAthena <c>status_get_atk</c>.</summary>
    int GetAtk(Entity bl, byte flag);

    /// <summary>rAthena <c>status_get_atk2</c>.</summary>
    int GetAtk2(Entity bl);

    /// <summary>rAthena <c>status_get_matk</c>.</summary>
    int GetMatk(Entity bl, byte flag);

    /// <summary>rAthena <c>status_get_def</c>.</summary>
    int GetDef(Entity bl);

    /// <summary>rAthena <c>status_get_def2</c>.</summary>
    int GetDef2(Entity bl);

    /// <summary>rAthena <c>status_get_mdef</c>.</summary>
    int GetMdef(Entity bl);

    /// <summary>rAthena <c>status_get_mdef2</c>.</summary>
    int GetMdef2(Entity bl);

    /// <summary>rAthena <c>status_get_hit</c>.</summary>
    int GetHit(Entity bl);

    /// <summary>rAthena <c>status_get_flee</c>.</summary>
    int GetFlee(Entity bl);

    /// <summary>rAthena <c>status_get_critical</c>.</summary>
    int GetCritical(Entity bl);

    /// <summary>rAthena <c>status_get_flee2</c>.</summary>
    int GetFlee2(Entity bl);

    /// <summary>rAthena <c>status_get_amotion</c>.</summary>
    int GetAmotion(Entity bl);

    /// <summary>rAthena <c>status_get_adelay</c>.</summary>
    int GetAdelay(Entity bl);

    /// <summary>rAthena <c>status_get_dmotion</c>.</summary>
    int GetDmotion(Entity bl);

    /// <summary>rAthena <c>status_get_speed</c>.</summary>
    int GetSpeed(Entity bl);

    /// <summary>rAthena <c>status_get_aspd_rate</c>.</summary>
    int GetAspdRate(Entity bl);

    // --- stat reads --------------------------------------------------
    /// <summary>rAthena <c>status_get_str</c>.</summary>
    int GetStr(Entity bl);
    /// <summary>rAthena <c>status_get_agi</c>.</summary>
    int GetAgi(Entity bl);
    /// <summary>rAthena <c>status_get_vit</c>.</summary>
    int GetVit(Entity bl);
    /// <summary>rAthena <c>status_get_int</c>.</summary>
    int GetInt(Entity bl);
    /// <summary>rAthena <c>status_get_dex</c>.</summary>
    int GetDex(Entity bl);
    /// <summary>rAthena <c>status_get_luk</c>.</summary>
    int GetLuk(Entity bl);

    // --- identity / mode helpers -------------------------------------
    /// <summary>rAthena <c>status_get_class</c>.</summary>
    int GetClass(Entity bl);
    /// <summary>rAthena <c>status_get_class_</c>.</summary>
    int GetClassUnderscore(Entity bl);
    /// <summary>rAthena <c>status_get_size</c>.</summary>
    byte GetSize(Entity bl);
    /// <summary>rAthena <c>status_get_race</c>.</summary>
    byte GetRace(Entity bl);
    /// <summary>rAthena <c>status_get_race2</c>.</summary>
    byte GetRace2(Entity bl);
    /// <summary>rAthena <c>status_get_element</c>.</summary>
    byte GetElement(Entity bl);
    /// <summary>rAthena <c>status_get_element_level</c>.</summary>
    byte GetElementLevel(Entity bl);
    /// <summary>rAthena <c>status_get_attack_element</c>.</summary>
    byte GetAttackElement(Entity bl);
    /// <summary>rAthena <c>status_get_attack_sc_element</c>.</summary>
    byte GetAttackScElement(Entity bl);
    /// <summary>rAthena <c>status_get_mode</c>.</summary>
    int GetMode(Entity bl);
    /// <summary>rAthena <c>status_has_mode</c>.</summary>
    bool HasMode(Entity bl, int mode);

    // --- HP/SP/AP read -----------------------------------------------
    /// <summary>rAthena <c>status_get_hp</c>.</summary>
    int GetHp(Entity bl);
    /// <summary>rAthena <c>status_get_max_hp</c>.</summary>
    int GetMaxHp(Entity bl);
    /// <summary>rAthena <c>status_get_sp</c>.</summary>
    int GetSp(Entity bl);
    /// <summary>rAthena <c>status_get_max_sp</c>.</summary>
    int GetMaxSp(Entity bl);
    /// <summary>rAthena <c>status_get_lv</c>.</summary>
    int GetLv(Entity bl);

    // ST.6 — companion-id accessors.
    /// <summary>rAthena <c>status_get_homid</c>.</summary>
    int GetHomId(Entity bl);
    /// <summary>rAthena <c>status_get_petid</c>.</summary>
    int GetPetId(Entity bl);
    /// <summary>rAthena <c>status_get_mercid</c>.</summary>
    int GetMercId(Entity bl);
    /// <summary>rAthena <c>status_get_eleid</c>.</summary>
    int GetEleId(Entity bl);

    // ST.6 — HP/SP/Max setters with overflow + display refresh.
    /// <summary>rAthena <c>status_set_hp</c>.</summary>
    void SetHp(Entity bl, int hp);
    /// <summary>rAthena <c>status_set_maxhp</c>.</summary>
    void SetMaxHp(Entity bl, int maxHp);
    /// <summary>rAthena <c>status_set_sp</c>.</summary>
    void SetSp(Entity bl, int sp);
    /// <summary>rAthena <c>status_set_maxsp</c>.</summary>
    void SetMaxSp(Entity bl, int maxSp);

    // --- regen / refresh ---------------------------------------------
    /// <summary>rAthena <c>status_natural_heal</c>.</summary>
    int NaturalHeal(long tick);
    /// <summary>rAthena <c>status_calc_regen</c>.</summary>
    void CalcRegen(Entity bl);
    /// <summary>rAthena <c>status_calc_regen_rate</c>.</summary>
    void CalcRegenRate(Entity bl);
    /// <summary>rAthena <c>status_change_clear</c>.</summary>
    int ChangeClear(Entity bl, byte type);
    /// <summary>rAthena <c>status_change_clear_buffs</c>.</summary>
    void ChangeClearBuffs(Entity bl, byte type);
    /// <summary>rAthena <c>status_change_clear_debuffs</c>.</summary>
    void ChangeClearDebuffs(Entity bl);

    // --- SC engine forwarders (real: IStatusChangeService) -----------
    /// <summary>rAthena <c>status_change_start</c> — forwards onto IStatusChangeService.</summary>
    int ChangeStart(Entity src, Entity bl, int type, int rate, int val1, int val2, int val3, int val4, int duration, byte flag);
    /// <summary>rAthena <c>status_change_end</c> — forwards.</summary>
    int ChangeEnd(Entity bl, int type, int timerId);
    /// <summary>rAthena <c>status_get_sc</c>.</summary>
    object? GetSc(Entity bl);
    /// <summary>rAthena <c>status_check_skilluse</c>.</summary>
    bool CheckSkillUse(Entity src, Entity target, ushort skillId, byte flag);
    /// <summary>rAthena <c>status_isdead</c>.</summary>
    bool IsDead(Entity bl);
    /// <summary>rAthena <c>status_isimmune</c>.</summary>
    bool IsImmune(Entity bl);

    // --- DB load / reload --------------------------------------------
    /// <summary>rAthena <c>status_readdb</c>.</summary>
    void ReadDb();
    /// <summary>rAthena <c>status_db_reload</c>.</summary>
    void DbReload();
}
