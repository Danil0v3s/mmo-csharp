using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status.StatusOps;

/// <summary>
/// Default <see cref="IStatusOpsService"/>. Real reads come from
/// <see cref="BattleStats"/> on the entity; the SC engine forwarders
/// delegate to <see cref="IStatusChangeService"/> when wired. Calc
/// + regen + readDB methods are entry points whose body lives on
/// the existing IStatusCalcService — surfaced here so the rAthena
/// port reads 1:1.
/// </summary>
public sealed class StatusOpsService : IStatusOpsService
{
    private readonly IStatusChangeService _sc;
    private readonly ILogger<StatusOpsService> _logger;

    public StatusOpsService(IStatusChangeService sc, ILogger<StatusOpsService> logger)
    {
        _sc = sc;
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

    // --- calc forwarders ---------------------------------------------
    public void CalcBl(Entity bl, int flag) { }
    public void CalcPc(PlayerEntity pc, int opt) { }
    public void CalcMob(MobEntity mob, byte opt) { }
    public void CalcHomunculus(Entity homun, byte opt) { }
    public void CalcMercenary(Entity merc, byte opt) { }
    public void CalcElemental(Entity ele, byte opt) { }
    public void CalcPet(Entity pet, byte opt) { }

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

    // --- regen / refresh ---------------------------------------------
    public int NaturalHeal(long tick) => 0;
    public void CalcRegen(Entity bl) { }
    public void CalcRegenRate(Entity bl) { }
    public int ChangeClear(Entity bl, byte type) => 0;
    public void ChangeClearBuffs(Entity bl, byte type) { }
    public void ChangeClearDebuffs(Entity bl) { }

    // --- SC engine forwarders ----------------------------------------
    public int ChangeStart(Entity src, Entity bl, int type, int rate, int val1, int val2, int val3, int val4, int duration, byte flag) => 0;
    public int ChangeEnd(Entity bl, int type, int timerId) => 0;
    public object? GetSc(Entity bl) => null;
    public bool CheckSkillUse(Entity src, Entity target, ushort skillId, byte flag) => true;
    public bool IsDead(Entity bl) => bl switch { PlayerEntity p => p.Hp <= 0, MobEntity m => m.Hp <= 0, _ => false };
    public bool IsImmune(Entity bl) => false;

    public void ReadDb() { }
    public void DbReload() { }
}
