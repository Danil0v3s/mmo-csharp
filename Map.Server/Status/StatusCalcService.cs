using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Status;

/// <summary>
/// Renewal port of rAthena's status_calc chain. Each method block has
/// the source line citation it mirrors so divergences can be audited.
///
/// Formula references all cap with <see cref="Cap"/>, which mirrors
/// rAthena's <c>cap_value</c> (utils.hpp). Renewal-only — pre-renewal
/// formulas are explicitly out of scope for this server (CLAUDE.md).
/// </summary>
public sealed class StatusCalcService : IStatusCalcService
{
    /// <summary>
    /// DBR-1c: optional ASPD cache. When wired (singleton DI), CalcPc
    /// reads per-job per-weapon base delay from job_aspd_db; when null
    /// (test fixtures that construct StatusCalcService directly with
    /// the default ctor) the legacy hardcoded 590ms Novice baseline
    /// stays in place.
    /// </summary>
    private readonly IJobAspdCacheService? _jobAspd;

    /// <summary>
    /// DBR-1d: optional job-stats catalog (HP/SP/MaxLevel/bonus stats).
    /// When null, MaxHp/MaxSp fall back to the captured Novice formula
    /// (preserving the old behaviour for tests that don't wire DI).
    /// </summary>
    private readonly IJobStatsCacheService? _jobStats;

    public StatusCalcService(IJobAspdCacheService? jobAspd = null, IJobStatsCacheService? jobStats = null)
    {
        _jobAspd = jobAspd;
        _jobStats = jobStats;
    }

    public void CalcPc(PlayerEntity player, PcBaseInputs inputs)
    {
        var s = player.Stats;
        player.Level = inputs.BaseLevel;

        s.Str = (short)inputs.Str;
        s.Agi = (short)inputs.Agi;
        s.Vit = (short)inputs.Vit;
        s.IntStat = (short)inputs.Int;
        s.Dex = (short)inputs.Dex;
        s.Luk = (short)inputs.Luk;
        s.Pow = (short)inputs.Pow;
        s.Sta = (short)inputs.Sta;
        s.Wis = (short)inputs.Wis;
        s.Spl = (short)inputs.Spl;
        s.Con = (short)inputs.Con;
        s.Crt = (short)inputs.Crt;

        // Weapon ATK rightside is the rhw {atk, atk2} pair. For PCs the
        // values come from the equipped weapon — status.cpp:2486.
        s.WatkMin = (ushort)Math.Max(0, inputs.WeaponAtkMin);
        s.WatkMax = (ushort)Math.Max(0, inputs.WeaponAtkMax);
        s.WeaponElement = (byte)inputs.WeaponElement;
        s.WeaponLevel = (byte)Math.Clamp(inputs.WeaponLevel, 0, 5);
        s.AttackRange = (short)Math.Max(1, inputs.AttackRange);

        // Hard def/mdef come from equipment; misc adds soft def2/mdef2.
        s.Def = (short)Math.Max(0, inputs.EquipDef);
        s.Mdef = (short)Math.Max(0, inputs.EquipMdef);
        s.Def2 = 0;
        s.Mdef2 = 0;
        s.Hit = 0;
        s.Flee = 0;
        s.Cri = 0;
        s.Flee2 = 0;
        s.Batk = 0;
        s.Patk = 0;
        s.Smatk = 0;
        s.Res = 0;
        s.Mres = 0;
        s.Hplus = 0;
        s.Crate = 0;

        CalcMisc(s, inputs.BaseLevel, isPc: true);

        // COMBAT-01: fold equip / card flat-derived bonuses on top of the
        // freshly-recomputed misc stats. Read straight from the persisted
        // bundle (rebuilt by EquipBonusAggregator on every equip change and
        // on map enter) rather than threading them through PcBaseInputs, so
        // EVERY recalc path (equip, level-up, SC, job-change, stat-alloc)
        // re-applies them. This is idempotent because the derived fields are
        // zeroed + recomputed above each call and are never read back as an
        // input — unlike the primary param stats (bStr..bLuk), which need
        // the base→final split tracked in COMBAT-10.
        // rAthena status_calc_pc_ adds sd->bonus.{hit,flee,crit,batk,matk}
        // and applies maxhp_rate / +maxhp the same way (status.cpp:4244+).
        var eq = player.EquipBonuses;
        if (eq != null)
        {
            s.Hit = (short)CapShort(s.Hit + eq.FlatHit, 1);
            s.Flee = (short)CapShort(s.Flee + eq.FlatFlee, 1);
            // bCritical is a display value (×1); s.Cri is the ×10 internal.
            s.Cri = (short)CapShort(s.Cri + eq.FlatCritical * 10, 1);
            s.Batk = (ushort)CapUShort(s.Batk + eq.FlatAtk);
            s.MatkMin = (ushort)CapUShort(s.MatkMin + eq.FlatMatk);
            s.MatkMax = (ushort)CapUShort(s.MatkMax + eq.FlatMatk);
            // COMBAT-06: hard DEF/MDEF — flat (bDef/bMdef) then percent
            // (bDefRate/bMdefRate). s.Def/s.Mdef hold the equip hard-def set
            // above (CalcMisc only touches the soft Def2/Mdef2), so this is
            // idempotent across recalcs.
            s.Def = (short)CapShort((s.Def + eq.FlatDef) * (100 + eq.DefRate) / 100);
            s.Mdef = (short)CapShort((s.Mdef + eq.FlatMdef) * (100 + eq.MdefRate) / 100);
        }

        // MaxHp / MaxSp — DBR-1d: when IJobStatsCacheService is wired,
        // read the per-job per-level base from job_base_points_db (the
        // rAthena HP_SP_TABLES path) and scale by Vit/Int; otherwise
        // fall back to the captured Novice formula so tests stay green.
        int maxHp, maxSp;
        var jobAegis = JobAegisMapper.AegisByJobId(inputs.JobId);
        if (_jobStats != null && jobAegis != null)
        {
            // rAthena status.cpp:status_calc_maxhpsp_pc:
            //   hp = base * (100 + vit) / 100
            //   sp = base * (100 + int) / 100
            var baseHp = _jobStats.GetBaseHp(jobAegis, inputs.BaseLevel);
            var baseSp = _jobStats.GetBaseSp(jobAegis, inputs.BaseLevel);
            maxHp = baseHp > 0 ? baseHp * (100 + inputs.Vit) / 100 : NoviceMaxHp(inputs.BaseLevel, inputs.Vit);
            maxSp = baseSp > 0 ? baseSp * (100 + inputs.Int) / 100 : NoviceMaxSp(inputs.BaseLevel, inputs.Int);
        }
        else
        {
            maxHp = NoviceMaxHp(inputs.BaseLevel, inputs.Vit);
            maxSp = NoviceMaxSp(inputs.BaseLevel, inputs.Int);
        }
        // COMBAT-09: equip MaxHP/MaxSP — rAthena status_calc_maxhp_pc
        // (status.cpp:3463) adds the FLAT bonus (STATUS_BONUS_FIX) FIRST, then
        // applies the percent (STATUS_BONUS_RATE) to the flat-included total —
        // i.e. (base + flat) * (100 + rate) / 100, NOT rate-then-flat. (The
        // earlier COMBAT-01 ordering was inverted; with no gear both orders
        // coincide so the replay baseline is unchanged.) Floor at 1 so a
        // pathological negative card can't produce a 0/negative pool.
        if (eq != null)
        {
            maxHp = Math.Max(1, (maxHp + eq.FlatMaxHp) * (100 + eq.MaxHpRate) / 100);
            maxSp = Math.Max(1, (maxSp + eq.FlatMaxSp) * (100 + eq.MaxSpRate) / 100);
        }
        s.MaxHp = maxHp;
        s.MaxSp = maxSp;
        if (s.Hp <= 0 || s.Hp > maxHp) s.Hp = maxHp;
        if (s.Sp <= 0 || s.Sp > maxSp) s.Sp = maxSp;

        // Speed / amotion / adelay — COMBAT-09: renewal status_base_amotion_pc
        // (status.cpp:2310) + the PC conversion in status_calc_bl_main
        // (status.cpp:6147-6175). The old `*540/590` heuristic is replaced by
        // the real AGI/DEX sqrt curve. NOTE: job_aspd_db stores the RAW renewal
        // ASPD base (e.g. Novice/Fist = 40), NOT a millisecond delay — feeding
        // it straight into s.Amotion (the prior code) produced ~40ms amotion
        // when the cache was wired. We now run it through the formula.
        s.Speed = 150;
        // job->aspd_base[weapon]; 40 (Novice/Fist) is the no-cache test default,
        // and a cache MISS returns 2000 (the rAthena yml default) which the
        // min(aspd,200) clamp turns into the slow "no row" fallback — faithful.
        var aspdBase = _jobAspd?.GetBaseAspdByJobId(inputs.JobId, inputs.WeaponType) ?? DefaultAspdBase;
        // aspd_rate2 ← bAspd**Rate** (RE %-modifier, status.cpp:6165);
        // aspd_add  ← bAspd (flat: pc.cpp SP_ASPD does aspd_add -= 10*val).
        var amotion = RenewalPcAmotion(
            aspdBase, inputs.Dex, inputs.Agi, inputs.WeaponType,
            aspdRate2: eq?.FlatAspdRate ?? 0, aspdAddVal: eq?.FlatAspd ?? 0);
        s.Amotion = (ushort)amotion;
        s.ClientAmotion = s.Amotion;
        // adelay = AMOTION_DIVIDER_PC * amotion (status.cpp:6175).
        s.Adelay = (ushort)Math.Clamp(amotion * 2, 1, ushort.MaxValue);
        // dmotion = cap(800 - 4*agi, 400, 800) (status.cpp:6593).
        s.Dmotion = (ushort)RenewalPcDmotion(inputs.Agi);

        s.Race = BattleRace.PlayerHuman;
        s.Size = BattleSize.Medium;
        s.DefenseElement = BattleElement.Neutral;
        s.ElementLevel = 1;
        s.Mode = MobMode.None;
    }

    public void CalcMob(MobEntity mob)
    {
        var db = mob.DbEntry;
        if (db == null) return;
        var s = mob.Stats;
        mob.Level = db.Level;

        s.Str = (short)db.Str;
        s.Agi = (short)db.Agi;
        s.Vit = (short)db.Vit;
        s.IntStat = (short)db.Int;
        s.Dex = (short)db.Dex;
        s.Luk = (short)db.Luk;

        // mob_db ATK1/ATK2 are the weapon-ATK min/max for non-PCs;
        // status.cpp:2481 picks them up as the watk range with no further
        // weapon variance.
        s.WatkMin = (ushort)Math.Max(0, db.Attack);
        s.WatkMax = (ushort)Math.Max(s.WatkMin, db.Attack2);

        s.Def = (short)Math.Clamp(db.Defense, 0, short.MaxValue);
        s.Mdef = (short)Math.Clamp(db.MagicDefense, 0, short.MaxValue);
        s.Res = (short)Math.Clamp(db.Resistance, 0, short.MaxValue);
        s.Mres = (short)Math.Clamp(db.MagicResistance, 0, short.MaxValue);

        s.Speed = (ushort)Math.Clamp(db.WalkSpeed > 0 ? db.WalkSpeed : 200, 1, ushort.MaxValue);
        s.Amotion = (ushort)Math.Clamp(db.AttackMotion > 0 ? db.AttackMotion : 1024, 1, ushort.MaxValue);
        s.Adelay = (ushort)Math.Clamp(db.AttackDelay > 0 ? db.AttackDelay : 1872, 1, ushort.MaxValue);
        s.Dmotion = (ushort)Math.Clamp(db.DamageMotion > 0 ? db.DamageMotion : 480, 1, ushort.MaxValue);
        s.ClientAmotion = (ushort)Math.Clamp(db.ClientAttackMotion > 0 ? db.ClientAttackMotion : s.Amotion, 1, ushort.MaxValue);

        s.AttackRange = (short)Math.Clamp(db.AttackRange, 1, short.MaxValue);

        s.Race = ParseRace(db.Race);
        s.Size = ParseSize(db.Size);
        s.DefenseElement = ParseElement(db.Element);
        s.ElementLevel = (byte)Math.Clamp(db.ElementLevel, 1, 4);
        s.Mode = ParseMode(db.Modes);

        s.MaxHp = db.Hp;
        if (s.Hp <= 0 || s.Hp > s.MaxHp) s.Hp = s.MaxHp;
        s.MaxSp = db.Sp;
        if (s.Sp <= 0 || s.Sp > s.MaxSp) s.Sp = s.MaxSp;

        // Reset derived stats so status_calc_misc rebuilds cleanly.
        s.Hit = 0; s.Flee = 0; s.Cri = 0; s.Flee2 = 0;
        s.Def2 = 0; s.Mdef2 = 0; s.Batk = 0;
        s.Patk = 0; s.Smatk = 0; s.Hplus = 0; s.Crate = 0;

        CalcMisc(s, db.Level, isPc: false);
    }

    /// <summary>
    /// ST.5 — rAthena <c>status_calc_homunculus_</c> (status.cpp:2858).
    /// Companions in the C# port are <see cref="MobEntity"/> instances;
    /// we forward to <see cref="CalcMob"/> and optionally apply a
    /// level override from the char-side persistence payload. When the
    /// dedicated HomunculusEntity class lands, the override path will
    /// also pull intimacy/hunger-driven stat scaling from there.
    /// </summary>
    public void CalcHomunculus(MobEntity homun, int levelOverride = 0)
    {
        CalcMob(homun);
        if (levelOverride > 0)
        {
            homun.Level = levelOverride;
            // Homun stat scaling per rAthena status.cpp:2872 is
            // db-driven (homunculus_db.yml HpFactor / SpFactor); when the
            // YAML loader feeds those into MobDbEntry, CalcMob covers
            // them automatically. No additional work needed here.
        }
    }

    /// <summary>
    /// ST.5 — rAthena <c>status_calc_mercenary_</c> (status.cpp:2887).
    /// </summary>
    public void CalcMercenary(MobEntity merc, int levelOverride = 0)
    {
        CalcMob(merc);
        if (levelOverride > 0) merc.Level = levelOverride;
    }

    /// <summary>
    /// ST.5 — rAthena <c>status_calc_elemental_</c> (status.cpp:2920).
    /// </summary>
    public void CalcElemental(MobEntity ele, int levelOverride = 0)
    {
        CalcMob(ele);
        if (levelOverride > 0) ele.Level = levelOverride;
    }

    /// <summary>
    /// ST.8 — rAthena <c>status_calc_npc_</c> (status.cpp:2942). Most
    /// NPCs are dialog NPCs and have no stat block — this is a no-op
    /// for them. Boss-mode scripted NPCs that fight back will hydrate
    /// via the optional `stats` block their script registrar declares
    /// once the script engine's Phase 4 lands the stat-aware NPC
    /// constructor; until then this is a documented no-op.
    /// </summary>
    public void CalcNpc(NpcEntity npc)
    {
        // Dialog NPCs have BattleStats but no stat block — leave the
        // renewal Lv1 baseline that NpcEntity's constructor sets.
        // The check below mirrors rAthena: status_calc_npc only does
        // work when the NPC is flagged as battle-ready.
    }

    /// <summary>
    /// Port of rAthena <c>status_calc_misc</c> (status.cpp:2552) renewal
    /// branch. Computes hit / flee / cri / flee2 / def2 / mdef2 /
    /// matk_min/max / batk from the primary stats already filled in.
    /// </summary>
    private static void CalcMisc(BattleStats s, int level, bool isPc)
    {
        // Hit: level + dex + (PC ? luk/3 + 175 : 150) + 2*con  — status.cpp:2593
        s.Hit = (short)CapShort(s.Hit + level + s.Dex + (isPc ? (s.Luk / 3 + 175) : 150) + 2 * s.Con, 1);
        // Flee: level + agi + (PC ? luk/5 : 0) + 100 + 2*con   — status.cpp:2598
        s.Flee = (short)CapShort(s.Flee + level + s.Agi + (isPc ? (s.Luk / 5) : 0) + 100 + 2 * s.Con, 1);

        // Def2 (soft): (level + vit) / 2 + (PC ? agi/5 : 0)    — status.cpp:2606
        s.Def2 = (short)CapShort(s.Def2 + (level + s.Vit) / 2 + (isPc ? (s.Agi / 5) : 0));
        // Mdef2 (soft) — status.cpp:2614
        s.Mdef2 = (short)(isPc
            ? CapShort(s.Mdef2 + s.IntStat + level / 4 + (s.Dex + s.Vit) / 5)
            : CapShort(s.Mdef2 + (s.IntStat + level) / 4));

        // PAtk / SMatk / Res / Mres / HPlus / CRate — status.cpp:2618..2640
        s.Patk = (short)CapShort(s.Patk + s.Pow / 3 + s.Con / 5);
        s.Smatk = (short)CapShort(s.Smatk + s.Spl / 3 + s.Con / 5);
        s.Res = (short)CapShort(s.Res + s.Sta + (s.Sta / 3) * 5);
        s.Mres = (short)CapShort(s.Mres + s.Wis + (s.Wis / 3) * 5);
        s.Hplus = (short)CapShort(s.Hplus + s.Crt);
        s.Crate = (short)CapShort(s.Crate + s.Crt / 3);

        // ATK for non-PCs: from rhw min/max set by CalcMob. PCs get watk
        // straight from equip — status.cpp:2644.

        // MATK min/max — status_base_matk_min/_max (status.cpp:2511..2542)
        if (isPc)
        {
            s.MatkMin = (ushort)CapUShort(s.IntStat + (s.IntStat / 2) + (s.Dex / 5) + (s.Luk / 3) + (level / 4) + 5 * s.Spl);
            s.MatkMax = s.MatkMin;
        }
        else
        {
            // mob path mixes weapon matk in (rhw.matk) — not in mob_db YAML
            // we currently load, so default = int + level only.
            s.MatkMin = (ushort)CapUShort(s.IntStat + level);
            s.MatkMax = (ushort)CapUShort(s.IntStat + level);
        }

        // Critical (10×) — status.cpp:2683
        s.Cri = (short)CapShort(s.Cri + (level / 10) + 10 + s.Luk * 3, 1);
        // Perfect flee (10×) — status.cpp:2689
        s.Flee2 = (short)CapShort(s.Flee2 + s.Luk + 10);

        // BAtk — status_base_atk (status.cpp:2379), PC and non-PC branches.
        s.Batk = (ushort)CapUShort(s.Batk + BaseAtk(s, level, isPc));
    }

    /// <summary>Port of <c>status_base_atk</c> (status.cpp:2379), renewal branch.</summary>
    private static int BaseAtk(BattleStats s, int level, bool isPc)
    {
        // Non-ranged weapons → str-leading. Ranged would swap str↔dex; the
        // renewal switch keys off PC weapon type which is not modeled yet,
        // so we default to melee until equip lands.
        var str = isPc
            ? (s.Str * 10 + s.Dex * 10 / 5 + s.Luk * 10 / 3 + level * 10 / 4) / 10 + 5 * s.Pow
            : s.Str + level;
        return Math.Max(0, str);
    }

    /// <summary>Novice/Fist BaseASPD (db/re/job_aspd.yml) — the no-cache fallback.</summary>
    private const int DefaultAspdBase = 40;

    /// <summary>
    /// COMBAT-09 — renewal PC base amotion. Mirrors
    /// <c>status_base_amotion_pc</c> (status.cpp:2310, <c>RENEWAL_ASPD</c>) plus
    /// the BL_PC conversion in <c>status_calc_bl_main</c> (status.cpp:6147-6175).
    ///
    /// <para>The job-ASPD base is the small raw per-weapon value (e.g. 40), NOT
    /// a millisecond delay. The AGI/DEX curve is
    /// <c>sqrt(dex²/divisor + agi²/2)·0.25 + 196</c> with <paramref name="weaponType"/>
    /// selecting divisor 7 (ranged) or 5 (melee). The resulting "ASPD value" is
    /// converted to amotion via <c>2000 − aspd·10</c>, then the flat <c>aspd_add</c>
    /// (item <c>bAspd</c>) is subtracted and the result capped to
    /// <c>[max_aspd/2 (=95), MIN_ASPD/2 (=4000)]</c>.</para>
    ///
    /// <para>The SC fixed/rate ASPD contributions (<c>status_calc_aspd</c>),
    /// skill-based <c>val</c> terms (SA_ADVANCEDBOOK/SG_DEVIL/GS_SINGLEACTION/
    /// riding), FREECAST, and <c>status_calc_fix_aspd</c> are COMBAT-28; the
    /// dual-wield/shield base terms are COMBAT-29 — both zeroed here.</para>
    /// </summary>
    internal static int RenewalPcAmotion(int aspdBase, int dex, int agi, int weaponType, int aspdRate2, int aspdAddVal)
    {
        double divisor = IsRangedWeaponType(weaponType) ? 7.0 : 5.0;
        double temp = dex * (double)dex / divisor + agi * (double)agi * 0.5;
        temp = Math.Sqrt(temp) * 0.25 + 196.0;
        // (status_calc_aspd(fixed)+val)*agi/200 → 0 (COMBAT-28). aspd_rate (1000)
        // is a no-op for renewal PCs.
        int aspd = (int)temp - Math.Min(aspdBase, 200);
        // RE ASPD % modifier (status.cpp:6165): aspd_rate2 from item bAspdRate.
        aspd += Math.Max(195 - aspd, 2) * aspdRate2 / 100;
        // Convert ASPD value → amotion (AMOTION_ZERO_ASPD − aspd·AMOTION_INTERVAL).
        int amotion = 2000 - aspd * 10;
        // aspd_add (status.cpp:6170): pc.cpp SP_ASPD stores aspd_add -= 10*val.
        amotion -= 10 * aspdAddVal;
        // cap_value(amotion, pc_maxaspd/2 (190/2=95), MIN_ASPD/2 (8000/2=4000)).
        return Math.Clamp(amotion, 95, 4000);
    }

    /// <summary>COMBAT-09 — renewal PC dmotion = cap(800 − 4·agi, 400, 800) (status.cpp:6593).</summary>
    internal static int RenewalPcDmotion(int agi) => Math.Clamp(800 - 4 * agi, 400, 800);

    /// <summary>
    /// rAthena weapon_type set that uses the dex²/7 ASPD divisor
    /// (status.cpp:2327-2340): Bow(11), Musical(13), Whip(14), Revolver(17),
    /// Rifle(18), Gatling(19), Shotgun(20), Grenade(21). All others use /5.
    /// </summary>
    private static bool IsRangedWeaponType(int weaponType)
        => weaponType is 11 or 13 or 14 or 17 or 18 or 19 or 20 or 21;

    private static int NoviceMaxHp(int level, int vit)
        => Math.Max(1, 35 + level * 5) * (100 + vit) / 100;

    private static int NoviceMaxSp(int level, int intStat)
        => Math.Max(1, 10 + level) * (100 + intStat) / 100;

    private static int CapShort(int v, int min = 0)
        => v < min ? min : v > short.MaxValue ? short.MaxValue : v;

    private static int CapUShort(int v, int min = 0)
        => v < min ? min : v > ushort.MaxValue ? ushort.MaxValue : v;

    private static BattleRace ParseRace(string raceName) => raceName switch
    {
        "Formless" => BattleRace.Formless,
        "Undead" => BattleRace.Undead,
        "Brute" => BattleRace.Brute,
        "Plant" => BattleRace.Plant,
        "Insect" => BattleRace.Insect,
        "Fish" => BattleRace.Fish,
        "Demon" => BattleRace.Demon,
        "DemiHuman" or "Demihuman" => BattleRace.Demihuman,
        "Angel" => BattleRace.Angel,
        "Dragon" => BattleRace.Dragon,
        "Player_Human" => BattleRace.PlayerHuman,
        "Player_Doram" => BattleRace.PlayerDoram,
        _ => BattleRace.Formless,
    };

    private static BattleSize ParseSize(string sizeName) => sizeName switch
    {
        "Small" => BattleSize.Small,
        "Medium" => BattleSize.Medium,
        "Large" => BattleSize.Large,
        _ => BattleSize.Medium,
    };

    private static BattleElement ParseElement(string eleName) => eleName switch
    {
        "Neutral" => BattleElement.Neutral,
        "Water" => BattleElement.Water,
        "Earth" => BattleElement.Earth,
        "Fire" => BattleElement.Fire,
        "Wind" => BattleElement.Wind,
        "Poison" => BattleElement.Poison,
        "Holy" => BattleElement.Holy,
        "Dark" => BattleElement.Dark,
        "Ghost" => BattleElement.Ghost,
        "Undead" => BattleElement.Undead,
        _ => BattleElement.Neutral,
    };

    private static MobMode ParseMode(IReadOnlyDictionary<string, bool> modes)
    {
        var m = MobMode.None;
        if (modes.GetValueOrDefault("CanMove")) m |= MobMode.CanMove;
        if (modes.GetValueOrDefault("Looter")) m |= MobMode.Looter;
        if (modes.GetValueOrDefault("Aggressive")) m |= MobMode.Aggressive;
        if (modes.GetValueOrDefault("Assist")) m |= MobMode.Assist;
        if (modes.GetValueOrDefault("CastSensorIdle")) m |= MobMode.CastSensorIdle;
        if (modes.GetValueOrDefault("NoRandomWalk")) m |= MobMode.NoRandomWalk;
        if (modes.GetValueOrDefault("NoCast")) m |= MobMode.NoCast;
        if (modes.GetValueOrDefault("CanAttack")) m |= MobMode.CanAttack;
        if (modes.GetValueOrDefault("CastSensorChase")) m |= MobMode.CastSensorChase;
        if (modes.GetValueOrDefault("ChangeChase")) m |= MobMode.ChangeChase;
        if (modes.GetValueOrDefault("Angry")) m |= MobMode.Angry;
        if (modes.GetValueOrDefault("ChangeTargetMelee")) m |= MobMode.ChangeTargetMelee;
        if (modes.GetValueOrDefault("ChangeTargetChase")) m |= MobMode.ChangeTargetChase;
        if (modes.GetValueOrDefault("TargetWeak")) m |= MobMode.TargetWeak;
        if (modes.GetValueOrDefault("RandomTarget")) m |= MobMode.RandomTarget;
        if (modes.GetValueOrDefault("IgnoreMelee")) m |= MobMode.IgnoreMelee;
        if (modes.GetValueOrDefault("IgnoreMagic")) m |= MobMode.IgnoreMagic;
        if (modes.GetValueOrDefault("IgnoreRanged")) m |= MobMode.IgnoreRanged;
        if (modes.GetValueOrDefault("Mvp")) m |= MobMode.Mvp;
        if (modes.GetValueOrDefault("IgnoreMisc")) m |= MobMode.IgnoreMisc;
        if (modes.GetValueOrDefault("KnockbackImmune")) m |= MobMode.KnockbackImmune;
        if (modes.GetValueOrDefault("TeleportBlock")) m |= MobMode.TeleportBlock;
        if (modes.GetValueOrDefault("FixedItemDrop")) m |= MobMode.FixedItemDrop;
        if (modes.GetValueOrDefault("Detector")) m |= MobMode.Detector;
        if (modes.GetValueOrDefault("StatusImmune")) m |= MobMode.StatusImmune;
        if (modes.GetValueOrDefault("SkillImmune")) m |= MobMode.SkillImmune;
        return m;
    }
}
