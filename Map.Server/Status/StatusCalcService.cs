using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills;

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
    // COMBAT-28 — live SC list for the ASPD buff/debuff terms. Held lazily to
    // break the DI cycle StatusCalcService → IStatusChangeService → DamageService
    // → MobSpawnService → IStatusCalcService. Optional so test ctors keep working
    // (degrades to "no SC contribution" when null).
    private readonly Lazy<IStatusChangeService>? _scLazy;
    private IStatusChangeService? _sc => _scLazy?.Value;

    public StatusCalcService(IJobAspdCacheService? jobAspd = null, IJobStatsCacheService? jobStats = null,
        Lazy<IStatusChangeService>? sc = null)
    {
        _jobAspd = jobAspd;
        _jobStats = jobStats;
        _scLazy = sc;
    }

    public void CalcPc(PlayerEntity player, PcBaseInputs inputs)
    {
        var s = player.Stats;
        player.Level = inputs.BaseLevel;

        // COMBAT-10 — base→final primary/trait stat layering.
        // rAthena status_calc_pc_ (status.cpp:4205-4266):
        //   final = base(+job_bonus) + status.X(allocated) + param_bonus(card)
        //           + param_equip(equip)
        // Here the input X is the PERSISTED BASE (the recalc-input builders now
        // read PlayerEntity.BaseParams, not the conflated final). We add the
        // equip param bonus (EquipBonusBundle, captured by COMBAT-01) + the
        // per-job per-job-level bonus (job_bonus_stats_db via
        // IJobStatsCacheService.GetBonusSum).
        //
        // SC stat mods (Blessing +STR, AGI-Up +AGI, …) still mutate Stats.X
        // imperatively in their OnStart/OnEnd. To keep them alive across a
        // recalc we apply only the DELTA of the (base+equip+job) param-base
        // versus the snapshot last folded (player.AppliedParamBase); the SC
        // contribution layered on top of that base is left untouched. With no
        // gear / job bonus / SC the totals equal the old `s.X = inputs.X`, so
        // the replay baseline + StatusCalcServiceTests stay green.
        var eq = player.EquipBonuses;
        var jb = default(JobBonusStatsSum);
        if (_jobStats != null)
        {
            var aegisForBonus = JobAegisMapper.AegisByJobId(inputs.JobId);
            if (aegisForBonus != null) jb = _jobStats.GetBonusSum(aegisForBonus, inputs.JobLevel);
        }

        Span<int> paramBase = stackalloc int[PcBaseParams.Count];
        paramBase[0] = inputs.Str + (eq?.Str ?? 0) + jb.Str;
        paramBase[1] = inputs.Agi + (eq?.Agi ?? 0) + jb.Agi;
        paramBase[2] = inputs.Vit + (eq?.Vit ?? 0) + jb.Vit;
        paramBase[3] = inputs.Int + (eq?.IntStat ?? 0) + jb.Int;
        paramBase[4] = inputs.Dex + (eq?.Dex ?? 0) + jb.Dex;
        paramBase[5] = inputs.Luk + (eq?.Luk ?? 0) + jb.Luk;
        paramBase[6] = inputs.Pow + (eq?.Pow ?? 0) + jb.Pow;
        paramBase[7] = inputs.Sta + (eq?.Sta ?? 0) + jb.Sta;
        paramBase[8] = inputs.Wis + (eq?.Wis ?? 0) + jb.Wis;
        paramBase[9] = inputs.Spl + (eq?.Spl ?? 0) + jb.Spl;
        paramBase[10] = inputs.Con + (eq?.Con ?? 0) + jb.Con;
        paramBase[11] = inputs.Crt + (eq?.Crt ?? 0) + jb.Crt;

        // COMBAT-32 — absolute base-stat addends from passive skills + Super
        // Novice. rAthena status_calc_pc_ (status.cpp:4221-4241) adds these to
        // base_status alongside the job_bonus, before the allocated/card/equip
        // fold — so they flow into the final stat AND its derived hit/atk/matk.
        // Folding them into paramBase[] here means they ride the same COMBAT-10
        // delta snapshot (recomputed each CalcPc from the live skill levels), so
        // the layering stays idempotent across recalcs.
        ApplyPassiveBaseStatAddends(paramBase, player, inputs);

        var applied = player.AppliedParamBase;
        if (applied == null)
        {
            // First calc for this entity: set the final param stat absolutely
            // (there is no SC contribution yet) and record the snapshot.
            applied = new int[PcBaseParams.Count];
            for (var i = 0; i < PcBaseParams.Count; i++)
            {
                s[i] = (short)CapShort(paramBase[i]);
                applied[i] = paramBase[i];
            }
            player.AppliedParamBase = applied;
        }
        else
        {
            // Later calc: shift the final stat by the change in the param base
            // only, preserving any SC delta already layered on top.
            for (var i = 0; i < PcBaseParams.Count; i++)
            {
                s[i] = (short)CapShort(s[i] + paramBase[i] - applied[i]);
                applied[i] = paramBase[i];
            }
        }

        // Weapon ATK rightside is the rhw {atk, atk2} pair. For PCs the
        // values come from the equipped weapon — status.cpp:2486.
        s.WatkMin = (ushort)Math.Max(0, inputs.WeaponAtkMin);
        s.WatkMax = (ushort)Math.Max(0, inputs.WeaponAtkMax);
        s.WeaponElement = (byte)inputs.WeaponElement;
        s.WeaponLevel = (byte)Math.Clamp(inputs.WeaponLevel, 0, 5);
        s.AttackRange = (short)Math.Max(1, inputs.AttackRange);
        // COMBAT-18 — left-hand (lhw) weapon for dual wield. status.cpp:2486.
        s.LeftWatkMin = (ushort)Math.Max(0, inputs.LeftWeaponAtkMin);
        s.LeftWatkMax = (ushort)Math.Max(0, inputs.LeftWeaponAtkMax);
        s.LeftWeaponElement = (byte)inputs.LeftWeaponElement;
        s.LeftWeaponLevel = (byte)Math.Clamp(inputs.LeftWeaponLevel, 0, 5);
        s.LeftWeaponType = inputs.LeftWeaponType;

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
        // `eq` is the EquipBonusBundle resolved above for the param layering.
        if (eq != null)
        {
            s.Hit = (short)CapShort(s.Hit + eq.FlatHit, 1);
            s.Flee = (short)CapShort(s.Flee + eq.FlatFlee, 1);
            // bCritical is a display value (×1); s.Cri is the ×10 internal.
            s.Cri = (short)CapShort(s.Cri + eq.FlatCritical * 10, 1);
            // COMBAT-45 — bCriticalRate (SP_CRITICAL_RATE) is a PERCENT modifier on
            // crit (rAthena status.cpp:4389 status->cri *= (100+critical_rate)/100),
            // not a flat add — applied here in the stat calc, after the flat fold.
            if (eq.CriticalRate != 0)
                s.Cri = (short)CapShort(s.Cri * (100 + eq.CriticalRate) / 100, 1);
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

        // COMBAT-33 — re-fold active SCs' DERIVED-stat contributions now that
        // CalcMisc + the equip fold have rebuilt the derived fields (Hit/Flee/
        // Cri/Def/Def2/Mdef/Mdef2/Batk/…). rAthena re-applies every SCB_*
        // contribution each status_calc_pc_; the C# SC handlers mutate
        // BattleStats directly in OnStart, so without this re-fold Angelus
        // (+Def2), Provoke (Batk%/Def%) and the generated SCB_* set are wiped by
        // any recalc. Primary-stat SC mods survive via the COMBAT-10 param-base
        // delta (so OnRecalc deliberately skips them — no double-count). Runs
        // before the MaxHp/ASPD blocks below, which read only primary stats.
        _sc?.ReapplyDerivedStatMods(player);

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
            // COMBAT-10: scale by the FINAL Vit/Int (s.Vit/s.IntStat = base +
            // equip + job_bonus + SC), matching rAthena's use of
            // battle_status.vit — so a +VIT card / job-bonus VIT grows MaxHP.
            // With no gear/SC/job the final equals the base, baseline-safe.
            var baseHp = _jobStats.GetBaseHp(jobAegis, inputs.BaseLevel);
            var baseSp = _jobStats.GetBaseSp(jobAegis, inputs.BaseLevel);
            maxHp = baseHp > 0 ? baseHp * (100 + s.Vit) / 100 : NoviceMaxHp(inputs.BaseLevel, s.Vit);
            maxSp = baseSp > 0 ? baseSp * (100 + s.IntStat) / 100 : NoviceMaxSp(inputs.BaseLevel, s.IntStat);
        }
        else
        {
            maxHp = NoviceMaxHp(inputs.BaseLevel, s.Vit);
            maxSp = NoviceMaxSp(inputs.BaseLevel, s.IntStat);
        }
        // COMBAT-30 — transcendent ×1.25 / Taekwon-ranker ×3 (status.cpp:3479),
        // applied AFTER the VIT/INT scale and BEFORE the flat/rate equip fold.
        if (JobAegisMapper.IsTranscendent(inputs.JobId))
        {
            maxHp = maxHp * 125 / 100;
            maxSp = maxSp * 125 / 100;
        }
        else if (inputs.JobId == JobAegisMapper.TaekwonJobId && inputs.BaseLevel >= 90 && player.IsTaekwonRanker)
        {
            maxHp *= 3;
            maxSp *= 3;
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
        // COMBAT-45 — status_calc_speed (status.cpp:7790): DEFAULT_WALK_SPEED 150
        // scaled by speed_rate. bSpeedRate (non-stackable min) + bSpeedAddRate
        // (stackable) are stored as the NEGATIVE delta in the bundle, so a faster
        // item lowers the speed value. (SC speed table is COMBAT-65.)
        // COMBAT-65 — status_calc_speed (status.cpp:7787): the equip speed delta is folded
        // into the SC slow/fast max-accumulators (NOT added), then the SC speed table applies.
        var itemSpeedDelta = (eq?.SpeedRate ?? 0) + (eq?.SpeedAddRate ?? 0);
        s.Speed = ComputeScSpeed(player, itemSpeedDelta);
        // job->aspd_base[weapon]; 40 (Novice/Fist) is the no-cache test default,
        // and a cache MISS returns 2000 (the rAthena yml default) which the
        // min(aspd,200) clamp turns into the slow "no row" fallback — faithful.
        s.HasShield = inputs.HasShield;
        var aspdBase = _jobAspd?.GetBaseAspdByJobId(inputs.JobId, inputs.WeaponType) ?? DefaultAspdBase;
        // COMBAT-29 — shield/dual-wield base terms (status.cpp:2321-2325):
        //   + aspd_base[Shield=99] when a shield is worn, else
        //   + aspd_base[weaponType2] / 4 when the off-hand holds a different weapon.
        if (inputs.HasShield)
            aspdBase += _jobAspd?.GetBaseAspdExactByJobId(inputs.JobId, ShieldAspdType) ?? 0;
        else if (inputs.LeftWeaponType != Map.Server.Inventory.WeaponTypeCodes.Fist)
            aspdBase += (_jobAspd?.GetBaseAspdExactByJobId(inputs.JobId, inputs.LeftWeaponType) ?? 0) / 4;
        // aspd_rate2 ← bAspd**Rate** (RE %-modifier, status.cpp:6165);
        // aspd_add  ← bAspd (flat: pc.cpp SP_ASPD does aspd_add -= 10*val).
        // COMBAT-10: feed the FINAL Agi/Dex (s.Agi/s.Dex = base + equip +
        // job_bonus + SC) so AGI-Up / +AGI gear speed up attacks, matching
        // rAthena status_base_amotion_pc reading battle_status. Baseline-safe
        // (no gear/SC/job → final == base).
        // COMBAT-28 — SC ASPD contributions (status_calc_aspd fixed/rate +
        // status_calc_fix_aspd), re-summed from the live SC list each recalc.
        var (fixedSc, rateSc, fixAspd) = ComputeScAspd(player);
        // COMBAT-50 — skill `val` ASPD terms (status.cpp:2343-2353): summed with the
        // SC fixed term inside the `(status_calc_aspd(true)+val)*agi/200` stage.
        // COMBAT-69 — supply the per-class job-level cap (jobId→aegis→job_db.MaxJobLevel) so the
        // SG_DEVIL `pc_is_maxjoblv` half resolves for a maxed Star Gladiator.
        var devilAegis = JobAegisMapper.AegisByJobId(inputs.JobId);
        var maxJobLevel = (_jobStats != null && devilAegis != null) ? _jobStats.GetMaxJobLevel(devilAegis) : 0;
        var skillVal = ComputeSkillAspdVal(player, inputs.WeaponType, maxJobLevel);
        var amotion = RenewalPcAmotion(
            aspdBase, s.Dex, s.Agi, inputs.WeaponType,
            aspdRate2: eq?.FlatAspdRate ?? 0, aspdAddVal: eq?.FlatAspd ?? 0,
            fixedSc: fixedSc, rateSc: rateSc, fixAspd: fixAspd, skillVal: skillVal);
        // COMBAT-70 — precompute the SA_FREECAST-adjusted amotion (the `5*(lv+10)%` ASPD-base
        // scale applies only while mid-cast; AttackService selects it via FreecastAdelay). The
        // factor must hit the ASPD base inside RenewalPcAmotion, not the final adelay, so it is a
        // second pass through the same formula.
        var freecastLv = player.LearnedSkills.GetValueOrDefault(SkillIds.SA_FREECAST);
        var fcAmotion = freecastLv > 0
            ? RenewalPcAmotion(aspdBase, s.Dex, s.Agi, inputs.WeaponType,
                aspdRate2: eq?.FlatAspdRate ?? 0, aspdAddVal: eq?.FlatAspd ?? 0,
                fixedSc: fixedSc, rateSc: rateSc, fixAspd: fixAspd, skillVal: skillVal, freecastLv: freecastLv)
            : amotion;
        // COMBAT-50 — SC_OVERED_BOOST (status_calc_fix_aspd) overrides the final
        // amotion to a fixed ASPD (val3): amotion = AMOTION_ZERO_ASPD − val3·10. Overrides both
        // the normal and the freecast amotion (rAthena fix_aspd is the last word).
        if (_sc?.Get(player, StatusType.OveredBoost) is { Val3: > 0 } ob)
            amotion = fcAmotion = Math.Clamp(2000 - ob.Val3 * 10, 95, 4000);
        s.Amotion = (ushort)amotion;
        s.ClientAmotion = s.Amotion;
        // adelay = AMOTION_DIVIDER_PC * amotion (status.cpp:6175).
        s.Adelay = (ushort)Math.Clamp(amotion * 2, 1, ushort.MaxValue);
        s.FreecastAdelay = (ushort)Math.Clamp(fcAmotion * 2, 1, ushort.MaxValue);
        // dmotion = cap(800 - 4*agi, 400, 800) (status.cpp:6593).
        s.Dmotion = (ushort)RenewalPcDmotion(s.Agi);

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
    /// COMBAT-32 — absolute base-stat addends from passive skills + Super Novice
    /// (rAthena status_calc_pc_, status.cpp:4221-4241). Mutates the param-base
    /// span in place so the addends layer like the job/equip bonus (and ride the
    /// COMBAT-10 idempotent delta-fold). Indices: 0=STR 1=AGI 2=VIT 3=INT 4=DEX
    /// 5=LUK (the PcBaseParams ordering).
    /// </summary>
    private static void ApplyPassiveBaseStatAddends(Span<int> paramBase, PlayerEntity player, in PcBaseInputs inputs)
    {
        var skills = player.LearnedSkills;

        // Super Novice (incl. baby / expanded variants), never died, joblv >= 70
        // → all six base stats +10. rAthena gates on
        // `(class_ & MAPID_UPPERMASK) == MAPID_SUPER_NOVICE && (job_level >= 70
        // || class_ & JOBL_THIRD) && die_counter == 0` — no 3rd super-novice
        // class exists, so the joblv branch is the operative one. status.cpp:4222.
        if (IsSuperNovice(inputs.JobId) && inputs.JobLevel >= 70 && player.DieCounter == 0)
        {
            paramBase[0] += 10; // STR
            paramBase[1] += 10; // AGI
            paramBase[2] += 10; // VIT
            paramBase[3] += 10; // INT
            paramBase[4] += 10; // DEX
            paramBase[5] += 10; // LUK
        }

        // Absolute passive-skill modifiers (status.cpp:4232-4241).
        if (SkillLv(skills, SkillIds.BS_HILTBINDING) > 0)
            paramBase[0] += 1;                          // +1 STR
        var dragon = SkillLv(skills, SkillIds.SA_DRAGONOLOGY);
        if (dragon > 0)
            paramBase[3] += (dragon + 1) / 2;           // +1 INT / 2 lv
        var owl = SkillLv(skills, SkillIds.AC_OWL);
        if (owl > 0)
            paramBase[4] += owl;                        // +lv DEX
        var research = SkillLv(skills, SkillIds.RA_RESEARCHTRAP);
        if (research > 0)
            paramBase[3] += research;                   // +lv INT
        if (SkillLv(skills, SkillIds.SU_POWEROFLAND) > 0)
            paramBase[3] += 20;                         // +20 INT
    }

    private static int SkillLv(Dictionary<ushort, byte> skills, ushort id)
        => skills != null && skills.TryGetValue(id, out var lv) ? lv : 0;

    /// <summary>
    /// rAthena <c>(class_ &amp; MAPID_UPPERMASK) == MAPID_SUPER_NOVICE</c>. The C#
    /// <see cref="MapidClass"/> overloads its Upper bit (so a mask test is
    /// unreliable, as in COMBAT-30), so detect by the super-novice Aegis job-id
    /// set: <c>JOB_SUPER_NOVICE</c> (23), <c>JOB_SUPER_BABY</c> (4045),
    /// <c>JOB_SUPER_NOVICE_E</c> (4190), <c>JOB_SUPER_BABY_E</c> (4191).
    /// </summary>
    private static bool IsSuperNovice(int jobId)
        => jobId is 23 or 4045 or 4190 or 4191;

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
    // COMBAT-29 — job_aspd "Shield" weapon-type index (rAthena MAX_WEAPON_TYPE;
    // the importer maps "Shield" → 99).
    private const int ShieldAspdType = 99;

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
    internal static int RenewalPcAmotion(int aspdBase, int dex, int agi, int weaponType, int aspdRate2, int aspdAddVal,
        int fixedSc = 0, int rateSc = 0, int fixAspd = 0, int skillVal = 0, int freecastLv = 0)
    {
        double divisor = IsRangedWeaponType(weaponType) ? 7.0 : 5.0;
        double temp = dex * (double)dex / divisor + agi * (double)agi * 0.5;
        temp = Math.Sqrt(temp) * 0.25 + 196.0;
        // COMBAT-28/50 — (status_calc_aspd(fixed) + skill val) * agi / 200
        // (status.cpp:2343). `fixedSc` carries the Quagmire-gated quicken bonuses +
        // Madness/Berserk/ASPD-potion; `skillVal` carries the SA_ADVANCEDBOOK /
        // SG_DEVIL / GS_SINGLEACTION / riding terms (COMBAT-50).
        int aspd = (int)(temp + (double)(fixedSc + skillVal) * agi / 200.0) - Math.Min(aspdBase, 200);
        // COMBAT-50 — SA_FREECAST (status.cpp:6156, RENEWAL_ASPD): while casting, the
        // ASPD is scaled to 5*(lv+10)% (lv1 → 55%, lv10 → 100%). Applied before the
        // RE ASPD% modifier, matching rAthena's amotion-variable order.
        if (freecastLv > 0)
            aspd = aspd * 5 * (freecastLv + 10) / 100;
        // RE ASPD % modifier (status.cpp:6165): aspd_rate2 (item bAspdRate) +
        // status_calc_aspd(false) SC rate term (Steel Body / Defender / Gospel / …).
        aspd += Math.Max(195 - aspd, 2) * (aspdRate2 + rateSc) / 100;
        // Convert ASPD value → amotion (AMOTION_ZERO_ASPD − aspd·AMOTION_INTERVAL).
        int amotion = 2000 - aspd * 10;
        // aspd_add (status.cpp:6170): pc.cpp SP_ASPD stores aspd_add -= 10*val.
        amotion -= 10 * aspdAddVal;
        // COMBAT-28 — status_calc_fix_aspd (status.cpp:6172): flat amotion add.
        amotion -= fixAspd;
        // cap_value(amotion, pc_maxaspd/2 (190/2=95), MIN_ASPD/2 (8000/2=4000)).
        return Math.Clamp(amotion, 95, 4000);
    }

    /// <summary>
    /// COMBAT-65 — port of <c>status_calc_speed</c> (status.cpp:7787) over the common
    /// movement SCs. DEFAULT_WALK_SPEED 150 is scaled by a <c>speed_rate</c> built from a
    /// two-phase max-accumulator: a SLOW phase (<c>speed_rate += max(slowdowns)</c>) then a
    /// FAST phase (<c>speed_rate -= max(speedups)</c>, floored at 40). The equip speed delta
    /// (negative = faster) folds into the same max() accumulators, not additively. Fixed-speed
    /// overrides (Steel Body 200, Defender ≥200) apply last. Re-read from the live SC set each
    /// recalc (idempotent). The exotic-SC tail + the freecast / hiding-walk early branches are
    /// COMBAT-84.
    /// </summary>
    private ushort ComputeScSpeed(PlayerEntity pc, int itemSpeedDelta)
    {
        const int MinWalk = 20, MaxWalk = 1000; // mmo.hpp MIN/MAX_WALK_SPEED
        int speed = 150;                        // DEFAULT_WALK_SPEED

        var sc = _sc;
        if (sc == null)
            return (ushort)Math.Clamp(speed * (100 + itemSpeedDelta) / 100, MinWalk, MaxWalk);

        bool Has(StatusType t) => sc.Get(pc, t) != null;
        int Val(StatusType t, Func<Status.StatusChange, int> read) => sc.Get(pc, t) is { } e ? read(e) : 0;

        int speedRate = 100;

        // --- SLOW phase: speed_rate += max(slowdown %) (status.cpp:7815) ---
        int slow = 0;
        if (Has(StatusType.DecreaseAgi))   slow = Math.Max(slow, 25);
        if (Has(StatusType.Quagmire))      slow = Math.Max(slow, 50);
        if (Has(StatusType.Curse))         slow = Math.Max(slow, 300);
        slow = Math.Max(slow, Val(StatusType.Slowdown, e => e.Val1));      // Slow Potion
        slow = Math.Max(slow, Val(StatusType.Dontforgetme, e => e.Val3));
        if (itemSpeedDelta > 0) slow = Math.Max(slow, itemSpeedDelta);     // permanent item slowdown
        speedRate += slow;

        // --- FAST phase: speed_rate -= max(speedup %), floor 40 (status.cpp:7918) ---
        int fast = 0;
        fast = Math.Max(fast, Val(StatusType.Agiup, e => e.Val1));
        if (Has(StatusType.IncreaseAgi))   fast = Math.Max(fast, 25);
        fast = Math.Max(fast, 2 * Val(StatusType.Windwalk, e => e.Val1));
        if (Has(StatusType.Cartboost))     fast = Math.Max(fast, 20);
        if (Has(StatusType.Berserk))       fast = Math.Max(fast, 25);
        if (Has(StatusType.Run))           fast = Math.Max(fast, 55);
        if (Has(StatusType.FullThrottle))  fast = Math.Max(fast, 25);
        if (itemSpeedDelta < 0) fast = Math.Max(fast, -itemSpeedDelta);    // permanent item speedup
        speedRate -= fast;
        if (speedRate < 40) speedRate = 40;

        if (speedRate != 100) speed = speed * speedRate / 100;
        // Fixed-speed overrides (status.cpp:7982).
        if (Has(StatusType.Steelbody)) speed = 200;
        if (Has(StatusType.Defender))  speed = Math.Max(speed, 200);

        return (ushort)Math.Clamp(speed, MinWalk, MaxWalk);
    }

    /// <summary>
    /// COMBAT-28 — port of <c>status_calc_aspd(true)</c> / <c>status_calc_aspd(false)</c>
    /// / <c>status_calc_fix_aspd</c> (status.cpp:8006 / 6172) over the SCs present in
    /// <see cref="StatusType"/>. Returns (flat ASPD-point bonus, rate %, flat amotion
    /// reduction). Re-summed from the live SC set each recalc, so it is idempotent.
    /// </summary>
    private (int fixedSc, int rateSc, int fixAspd) ComputeScAspd(PlayerEntity pc)
    {
        if (_sc == null) return (0, 0, 0);
        int Val(StatusType t, Func<Status.StatusChange, int> read)
            => _sc.Get(pc, t) is { } sce ? read(sce) : 0;
        bool Has(StatusType t) => _sc.Get(pc, t) != null;

        // ---- status_calc_aspd(fixed=true): flat ASPD-point bonus ----
        int fixedSc = 0;
        // Quagmire gates off the quicken family (status.cpp:8016).
        if (!Has(StatusType.Quagmire))
        {
            if (Has(StatusType.Twohandquicken) || Has(StatusType.Spearquicken)
                || Has(StatusType.Adrenaline) || Has(StatusType.MercQuicken)) fixedSc = 7;
            else if (Has(StatusType.Adrenaline2)) fixedSc = 6;
            else if (Has(StatusType.Fleet)) fixedSc = 5;
        }
        int assn = Val(StatusType.Assncros, sc => sc.Val2); // renewal: always applies
        if (assn > fixedSc) fixedSc += assn;
        if (Has(StatusType.Madnesscancel) && fixedSc < 20) fixedSc = 20;
        else if (Has(StatusType.Berserk) && fixedSc < 15) fixedSc = 15;
        // ASPD potions stack on top (val1).
        foreach (var pot in new[] { StatusType.Aspdpotion3, StatusType.Aspdpotion2, StatusType.Aspdpotion1, StatusType.Aspdpotion0 })
            if (Has(pot)) { fixedSc += Val(pot, sc => sc.Val1); break; }

        // ---- status_calc_aspd(false): rate reduction % ----
        int rateSc = 0;
        rateSc -= Val(StatusType.Dontforgetme, sc => sc.Val2) / 10; // PC only — always a PC here
        if (Has(StatusType.Steelbody)) rateSc -= 25;
        rateSc -= Val(StatusType.Defender, sc => sc.Val4) / 10;
        if (_sc.Get(pc, StatusType.Gospel) is { Val4: 2 }) rateSc -= 75; // BCT_ENEMY
        // COMBAT-50 — rate-term positives (status.cpp:6206-6213): SwingDance val3,
        // IncAspdRate val1, GatlingFever val1.
        rateSc += Val(StatusType.Swingdance, sc => sc.Val3);
        rateSc += Val(StatusType.Incaspdrate, sc => sc.Val1);
        rateSc += Val(StatusType.Gatlingfever, sc => sc.Val1);

        // ---- status_calc_fix_aspd: flat amotion reduction ----
        int fixAspd = 0;
        fixAspd += Val(StatusType.HeatBarrel, sc => sc.Val1) * 10;
        // COMBAT-50 — exotic status_calc_fix_aspd terms (status.cpp:6172): +5 ASPD
        // (−50 amotion) for the Sorcerer element options, plus Fighting Spirit /
        // Soul Shadow / Sincere Faith. (SC_OVERED_BOOST overrides the whole amotion
        // and is handled in CalcPc.)
        if (Has(StatusType.GustOption) || Has(StatusType.BlastOption) || Has(StatusType.WildStormOption))
            fixAspd += 50;
        fixAspd += Val(StatusType.Fightingspirit, sc => sc.Val2);
        fixAspd += Val(StatusType.Soulshadow, sc => sc.Val2) * 10;
        fixAspd += Val(StatusType.SincereFaith, sc => sc.Val2) * 10;

        return (fixedSc, rateSc, fixAspd);
    }

    /// <summary>
    /// COMBAT-50 — the skill <c>val</c> ASPD terms (status.cpp:2343-2353): book /
    /// star-emperor / gun ASPD bonuses and the riding penalties, gated on the
    /// learned skill level + weapon / mount / class. Summed with the SC fixed term
    /// inside <c>(status_calc_aspd(true)+val)·agi/200</c>.
    /// </summary>
    internal static int ComputeSkillAspdVal(PlayerEntity pc, int weaponType, int maxJobLevel = 0)
    {
        int Lv(ushort id) => pc.LearnedSkills.GetValueOrDefault(id);
        int val = 0;

        // SA_ADVANCEDBOOK — Book weapon: +(lv-1)/2 + 1.
        int ab = Lv(SkillIds.SA_ADVANCEDBOOK);
        if (ab > 0 && weaponType == Map.Server.Inventory.WeaponTypeCodes.Book)
            val += (ab - 1) / 2 + 1;

        // SG_DEVIL — +1 + lv. rAthena gate (status.cpp:2345):
        //   (class & MAPID_THIRDMASK) == MAPID_STAR_EMPEROR || pc_is_maxjoblv(sd)
        // COMBAT-69 adds the `|| pc_is_maxjoblv` half: a Star Gladiator (Taekwon 2nd-class,
        // not yet Star Emperor) who is at their job-level cap also gets the bonus. maxJobLevel
        // is 0 when no job-stats cache is wired (callers without it keep the Star-Emperor-only
        // behavior).
        int dv = Lv(SkillIds.SG_DEVIL);
        if (dv > 0 && (IsStarEmperor(pc) || (maxJobLevel > 0 && pc.JobLevel >= maxJobLevel)))
            val += 1 + dv;

        // GS_SINGLEACTION — guns (W_REVOLVER..W_GRENADE): +(lv+1)/2.
        int gs = Lv(SkillIds.GS_SINGLEACTION);
        if (gs > 0 && weaponType >= Map.Server.Inventory.WeaponTypeCodes.Revolver
                   && weaponType <= Map.Server.Inventory.WeaponTypeCodes.Grenade)
            val += (gs + 1) / 2;

        // Riding penalties (mutually exclusive): Peco −50 + 10·KN_CAVALIERMASTERY;
        // dragon −25 + 5·RK_DRAGONTRAINING.
        if ((pc.Option & PlayerOption.Riding) != 0)
            val -= 50 - 10 * Lv(SkillIds.KN_CAVALIERMASTERY);
        else if (pc.Option.HasDragon())
            val -= 25 - 5 * Lv(SkillIds.RK_DRAGONTRAINING);

        return val;
    }

    /// <summary>True for a Star Emperor (Taekwon-tree 3rd class) — the
    /// <c>(class &amp; MAPID_THIRDMASK) == MAPID_STAR_EMPEROR</c> gate.</summary>
    private static bool IsStarEmperor(PlayerEntity pc)
        => (pc.ClassMask & MapidClass.FirstMask) == MapidClass.Taekwon
           && (pc.ClassMask & MapidClass.ThirdClass) != 0;

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
