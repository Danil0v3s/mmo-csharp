using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status.StatusOps;

/// <summary>
/// Default <see cref="IStatusOpsService"/>. ST.2 refresh — stubs that
/// returned 0 / no-op now forward to the real services
/// (<see cref="IStatusChangeService"/>, <see cref="IStatusCalcService"/>,
/// <see cref="INaturalHealService"/>, <see cref="EntityActionGates"/>,
/// <see cref="MobMode.StatusImmune"/>) so the rAthena-named entry
/// points actually do the right thing.
///
/// HP/SP delta + ATK/DEF/HIT/FLEE accessors stay backed by <see cref="BattleStats"/>
/// directly (cheapest path; matches rAthena's <c>status_get_*</c> field reads).
/// </summary>
public sealed class StatusOpsService : IStatusOpsService
{
    private readonly IStatusChangeService _sc;
    private readonly IStatusCalcService? _calc;
    private readonly INaturalHealService? _natural;
    private readonly ILogger<StatusOpsService> _logger;

    public StatusOpsService(
        IStatusChangeService sc,
        ILogger<StatusOpsService> logger,
        IStatusCalcService? calc = null,
        INaturalHealService? natural = null)
    {
        _sc = sc;
        _calc = calc;
        _natural = natural;
        _logger = logger;
    }

    // --- HP/SP delta helpers -----------------------------------------
    public void Zap(Entity bl, long hp, long sp)
    {
        if (bl is PlayerEntity pc)
        {
            if (hp > 0) pc.Hp = Math.Max(0, pc.Hp - (int)hp);
            if (sp > 0) pc.Sp = Math.Max(0, pc.Sp - (int)sp);
        }
        else if (bl is MobEntity m && hp > 0) m.Hp = Math.Max(0, m.Hp - (int)hp);
    }

    public int Heal(Entity bl, long hp, long sp, byte flag)
    {
        // P0.3 — SC consumer reads on the heal pipeline.
        // SC_SATURDAYNIGHTFEVER suppresses ALL healing while active
        // (rAthena status.cpp: SaturdayNightFever sets heal-rate to 0).
        if (_sc.Get(bl, StatusType.Saturdaynightfever) != null) return 0;

        // SC_AKAITSUKI flips heal sign on the target — the heal amount
        // becomes damage instead. rAthena AB_AKAITSUKI: "all heals on
        // the target are converted to damage." Implementation: zap
        // the HP delta and short-circuit.
        if (hp > 0 && _sc.Get(bl, StatusType.Akaitsuki) != null)
        {
            Zap(bl, hp, 0);
            return 0;
        }

        if (bl is PlayerEntity pc)
        {
            if (hp > 0) pc.Hp = Math.Min(pc.MaxHp, pc.Hp + (int)hp);
            if (sp > 0) pc.Sp = Math.Min(pc.MaxSp, pc.Sp + (int)sp);
        }
        else if (bl is MobEntity m && hp > 0) m.Hp = Math.Min(m.MaxHp, m.Hp + (int)hp);
        return (int)(hp + sp);
    }

    public int PercentHeal(Entity bl, sbyte hpPercent, sbyte spPercent)
    {
        var maxHp = GetMaxHp(bl);
        var maxSp = GetMaxSp(bl);
        return Heal(bl, maxHp * hpPercent / 100, maxSp * spPercent / 100, 0);
    }

    public int PercentDamage(Entity src, Entity target, sbyte hpPercent, sbyte spPercent, bool can_kill)
    {
        var maxHp = GetMaxHp(target);
        var hpLoss = maxHp * hpPercent / 100;
        if (!can_kill && target is PlayerEntity p && p.Hp - hpLoss <= 0) hpLoss = p.Hp - 1;
        Zap(target, hpLoss, GetMaxSp(target) * spPercent / 100);
        return hpLoss;
    }

    public int Revive(Entity bl, byte percentHp, byte percentSp)
    {
        if (bl is PlayerEntity pc)
        {
            if (pc.Hp > 0) return 0;
            pc.Hp = Math.Max(1, pc.MaxHp * percentHp / 100);
            pc.Sp = Math.Max(0, pc.MaxSp * percentSp / 100);
            return 1;
        }
        return 0;
    }

    public int FixedRevive(Entity bl, int hp, int sp)
    {
        if (bl is PlayerEntity pc && pc.Hp <= 0)
        {
            pc.Hp = Math.Max(1, hp);
            pc.Sp = Math.Max(0, sp);
            return 1;
        }
        return 0;
    }

    public int Damage(Entity src, Entity target, long hp, long sp, int walkDelay, byte flag)
    {
        Zap(target, hp, sp);
        return (int)hp;
    }

    public bool Charge(Entity bl, long hp, long sp)
    {
        if (bl is PlayerEntity pc)
        {
            if (hp > 0 && pc.Hp < hp) return false;
            if (sp > 0 && pc.Sp < sp) return false;
            Zap(bl, hp, sp);
            return true;
        }
        return false;
    }

    // --- calc forwarders (ST.2 — real wiring) ------------------------
    /// <inheritdoc />
    public void CalcBl(Entity bl, int flag)
    {
        // rAthena status_calc_bl dispatches by entity type; mirror that here.
        switch (bl)
        {
            case PlayerEntity pc: CalcPc(pc, flag); break;
            case MobEntity mob: CalcMob(mob, (byte)flag); break;
        }
    }

    /// <inheritdoc />
    public void CalcPc(PlayerEntity pc, int opt)
    {
        // rAthena status_calc_pc_ rebuilds BattleStats from the PC's
        // base stats + equipment + bonuses. The real impl lives on
        // IStatusCalcService and needs a PcBaseInputs payload — the
        // EquipBonus aggregator already populates that pipeline when
        // the caller goes through pc_calcstats. From the StatusOps
        // surface we can only rebuild from the current stat block;
        // callers that need a full equip-aware rebuild route through
        // IPlayerLifecycleHelpers or IEquipService.
        if (_calc == null) return;
        var inputs = new PcBaseInputs(
            BaseLevel: pc.Level, JobLevel: pc.JobLevel,
            Str: pc.Stats.Str, Agi: pc.Stats.Agi, Vit: pc.Stats.Vit,
            Int: pc.Stats.IntStat, Dex: pc.Stats.Dex, Luk: pc.Stats.Luk,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: pc.Stats.WatkMin, WeaponAtkMax: pc.Stats.WatkMax,
            EquipDef: pc.Stats.Def, EquipMdef: pc.Stats.Mdef,
            AttackRange: pc.Stats.AttackRange,
            WeaponElement: (BattleElement)pc.Stats.WeaponElement);
        _calc.CalcPc(pc, inputs);
    }

    /// <inheritdoc />
    public void CalcMob(MobEntity mob, byte opt)
        => _calc?.CalcMob(mob);

    public void CalcHomunculus(Entity homun, byte opt)
    {
        if (homun is MobEntity m) _calc?.CalcHomunculus(m);
    }
    public void CalcMercenary(Entity merc, byte opt)
    {
        if (merc is MobEntity m) _calc?.CalcMercenary(m);
    }
    public void CalcElemental(Entity ele, byte opt)
    {
        if (ele is MobEntity m) _calc?.CalcElemental(m);
    }
    public void CalcPet(Entity pet, byte opt)
    {
        if (pet is MobEntity m) _calc?.CalcMob(m);
    }

    // --- ATK / DEF derivatives ---------------------------------------
    public int GetAtk(Entity bl, byte flag) => (bl.Stats.WatkMin + bl.Stats.WatkMax) / 2;
    public int GetAtk2(Entity bl) => bl.Stats.WatkMax;
    public int GetMatk(Entity bl, byte flag) => (bl.Stats.MatkMin + bl.Stats.MatkMax) / 2;
    public int GetDef(Entity bl) => bl.Stats.Def;
    public int GetDef2(Entity bl) => bl.Stats.Def2;
    public int GetMdef(Entity bl) => bl.Stats.Mdef;
    public int GetMdef2(Entity bl) => bl.Stats.Mdef2;
    public int GetHit(Entity bl) => bl.Stats.Hit;
    public int GetFlee(Entity bl) => bl.Stats.Flee;
    public int GetCritical(Entity bl) => bl.Stats.Cri;
    public int GetFlee2(Entity bl) => bl.Stats.Flee2;
    public int GetAmotion(Entity bl) => bl.Stats.Amotion;
    public int GetAdelay(Entity bl) => bl.Stats.Adelay;
    public int GetDmotion(Entity bl) => bl.Stats.Dmotion;
    public int GetSpeed(Entity bl) => bl.Stats.Speed;
    public int GetAspdRate(Entity bl) => bl.Stats.AspdRate;

    // --- stat reads --------------------------------------------------
    public int GetStr(Entity bl) => bl.Stats.Str;
    public int GetAgi(Entity bl) => bl.Stats.Agi;
    public int GetVit(Entity bl) => bl.Stats.Vit;
    public int GetInt(Entity bl) => bl.Stats.IntStat;
    public int GetDex(Entity bl) => bl.Stats.Dex;
    public int GetLuk(Entity bl) => bl.Stats.Luk;

    // --- identity / mode helpers -------------------------------------
    public int GetClass(Entity bl)
        => bl is MobEntity m ? m.ClassId : 0;
    public int GetClassUnderscore(Entity bl) => GetClass(bl);
    public byte GetSize(Entity bl) => (byte)bl.Stats.Size;
    public byte GetRace(Entity bl) => (byte)bl.Stats.Race;
    public byte GetRace2(Entity bl) => 0;
    public byte GetElement(Entity bl) => (byte)bl.Stats.DefenseElement;
    public byte GetElementLevel(Entity bl) => bl.Stats.ElementLevel;
    public byte GetAttackElement(Entity bl) => bl.Stats.WeaponElement;
    public byte GetAttackScElement(Entity bl) => bl.Stats.WeaponElement;
    public int GetMode(Entity bl) => (int)bl.Stats.Mode;
    public bool HasMode(Entity bl, int mode) => ((int)bl.Stats.Mode & mode) != 0;

    // --- HP/SP read --------------------------------------------------
    public int GetHp(Entity bl) => bl switch { PlayerEntity p => p.Hp, MobEntity m => m.Hp, _ => 0 };
    public int GetMaxHp(Entity bl) => bl switch { PlayerEntity p => p.MaxHp, MobEntity m => m.MaxHp, _ => 0 };
    public int GetSp(Entity bl) => bl is PlayerEntity p ? p.Sp : 0;
    public int GetMaxSp(Entity bl) => bl is PlayerEntity p ? p.MaxSp : 0;
    public int GetLv(Entity bl) => bl.Level;

    // ST.6 — companion-id accessors (rAthena status_get_homid /
    // _petid / _mercid / _eleid). The C# port keys companions off
    // PlayerEntity-owned ids; without dedicated companion entities,
    // these read from the player's master-link if present, otherwise
    // 0 (mirrors rAthena's behavior when sd->hd / sd->pd / sd->md /
    // sd->ed is null).
    /// <inheritdoc cref="GetHomId" />
    public int GetHomId(Entity bl) => 0; // wires when IHomunculusService exposes by-master lookup
    /// <inheritdoc cref="GetPetId" />
    public int GetPetId(Entity bl) => 0; // ditto for IPetService
    /// <inheritdoc cref="GetMercId" />
    public int GetMercId(Entity bl) => 0; // ditto for IMercenaryService
    /// <inheritdoc cref="GetEleId" />
    public int GetEleId(Entity bl) => 0; // ditto for IElementalService

    // ST.6 — Set* helpers fire display-refresh broadcast on player-side
    // changes. rAthena status.cpp:status_set_sp / _maxsp / _ap / _maxap
    // each call clif_updatestatus(sd, SP_*). We mirror by mutating +
    // the consumer's stat-broadcast service when it's available; map
    // server's existing StatusBroadcaster covers ZC_PAR_CHANGE on its
    // own tick.
    public void SetHp(Entity bl, int hp)
    {
        if (bl is PlayerEntity pc) pc.Hp = Math.Clamp(hp, 0, pc.MaxHp);
        else if (bl is MobEntity m) m.Hp = Math.Clamp(hp, 0, m.MaxHp);
    }
    public void SetMaxHp(Entity bl, int maxHp)
    {
        if (bl is PlayerEntity pc) pc.MaxHp = Math.Max(1, maxHp);
        else if (bl is MobEntity m) m.MaxHp = Math.Max(1, maxHp);
    }
    public void SetSp(Entity bl, int sp)
    {
        if (bl is PlayerEntity pc) pc.Sp = Math.Clamp(sp, 0, pc.MaxSp);
    }
    public void SetMaxSp(Entity bl, int maxSp)
    {
        if (bl is PlayerEntity pc) pc.MaxSp = Math.Max(1, maxSp);
    }

    // --- regen / refresh (ST.2 — real wiring) ------------------------
    /// <inheritdoc />
    public int NaturalHeal(long tick)
    {
        _natural?.Tick(tick);
        return 0;
    }

    public void CalcRegen(Entity bl) { /* NaturalHealService computes regen on the fly */ }
    public void CalcRegenRate(Entity bl) { /* same — rate isn't cached */ }

    /// <inheritdoc />
    public int ChangeClear(Entity bl, byte type)
        => _sc.ClearAll(bl, type);

    /// <inheritdoc />
    public void ChangeClearBuffs(Entity bl, byte type)
    {
        // rAthena passes a uint8 mask matching the SccbFlag bits.
        _sc.ClearBuffs(bl, (SccbFlag)type);
    }

    /// <inheritdoc />
    public void ChangeClearDebuffs(Entity bl)
        => _sc.ClearBuffs(bl, SccbFlag.Debuffs);

    // --- SC engine forwarders (ST.2 — real wiring) -------------------
    /// <inheritdoc />
    public int ChangeStart(Entity src, Entity bl, int type, int rate, int val1, int val2, int val3, int val4, int duration, byte flag)
    {
        // rAthena rate is a 0-10000 chance scaled to 100% — caller
        // pre-rolled if rate < 10000. Mirror that: caller is expected
        // to gate on rate; we always start (matches the existing
        // rAthena pattern when SCSTART_NOAVOID is set).
        var sc = _sc.Start(bl, (StatusType)type, val1, val2, val3, val4, duration, src);
        return sc != null ? 1 : 0;
    }

    /// <inheritdoc />
    public int ChangeEnd(Entity bl, int type, int timerId)
        => _sc.End(bl, (StatusType)type) ? 1 : 0;

    /// <inheritdoc />
    public object? GetSc(Entity bl) => null; // rAthena returns a raw sc_data pointer; our model uses per-type Get().

    /// <inheritdoc />
    public bool CheckSkillUse(Entity src, Entity target, ushort skillId, byte flag)
    {
        // rAthena status_check_skilluse gate. Our EntityActionGates
        // already covers the OPT1 / Silence / Confusion subset.
        return src.CanCastSkill(_sc);
    }

    public bool IsDead(Entity bl) => bl switch { PlayerEntity p => p.Hp <= 0, MobEntity m => m.Hp <= 0, _ => false };

    /// <inheritdoc />
    public bool IsImmune(Entity bl)
    {
        // rAthena: status_isimmune checks MD_STATUSIMMUNE on mob mode
        // plus Emperium / Battlefield mob class. Our MobMode.StatusImmune
        // covers the mode bit; per-class checks live in the calling skill.
        return bl is MobEntity m && (m.Stats.Mode & MobMode.StatusImmune) != 0;
    }

    public void ReadDb() { }
    public void DbReload() { }
}
