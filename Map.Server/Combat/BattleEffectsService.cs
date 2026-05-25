using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleEffectsService"/>.
///
/// <para>VanishDamage + VellumDamage are real — they read the
/// target's MaxHP/MaxSP and apply the % drain via
/// <see cref="IDamageService.ApplyDamage"/>.</para>
///
/// <para>Drain + ammo + autocast read equip-bonus aggregators
/// (<c>sd->bonus.hp_drain_rate</c> etc.) that don't exist on
/// BattleStats yet; canonical entry points stay here so the
/// resolvers can call freely once the aggregator lands.</para>
/// </summary>
public sealed class BattleEffectsService : IBattleEffectsService
{
    private readonly IDamageService _damage;
    private readonly IStatusChangeService? _sc;
    private readonly IPlayerBonusService? _bonusSvc;
    private readonly ILogger<BattleEffectsService> _logger;

    public BattleEffectsService(
        IDamageService damage,
        ILogger<BattleEffectsService> logger,
        IStatusChangeService? sc = null,
        IPlayerBonusService? bonusSvc = null)
    {
        _damage = damage;
        _logger = logger;
        _sc = sc;
        _bonusSvc = bonusSvc;
    }

    public void Drain(PlayerEntity attacker, Entity target, long rDamage, long lDamage, int race, int classKind)
    {
        // No equip-bonus drain table yet. When the aggregator lands,
        // walk attacker.Stats.HpDrain[race] / SpDrain[race] / class
        // arrays and convert to delta on attacker.Hp / Sp. The
        // canonical entry is here so the resolver call site is correct.
    }

    public void ConsumeAmmo(PlayerEntity attacker, ushort skillId, ushort skillLevel)
    {
        // rAthena: pc_searcharrow + pc_delitem. Once the arrow slot is
        // tracked on PlayerEntity (it lives in equip_arrow today), this
        // method will find it and decrement amount by skill_db.AmmoQty.
    }

    public void AutocastAfterCast(PlayerEntity attacker, Entity target)
    {
        // Wave 65 — rAthena battle_autocast_aftercast (battle.cpp:6603).
        // Two layers:
        // (a) Autospell list — `bonus3 bAutoSpell, sid, lv, rate;` lives
        //     on IPlayerBonusService as OnHit autobonus entries.
        //     ExecuteAutobonus rolls each row and logs the fire event;
        //     full script execution lands with the script-engine port.
        // (b) AddEffOnAttack — `bonus3 bAddEff, sc, rate, dur;` from
        //     EquipBonusBundle. Each entry rolls per-myriad and starts
        //     the SC on the target (Mantis Stun, Wraith Card Curse, …).
        _bonusSvc?.ExecuteAutobonus(attacker, AutobonusTrigger.OnHit);

        var bundle = attacker.EquipBonuses;
        if (bundle == null || _sc == null) return;
        foreach (var entry in bundle.AddEffOnAttack)
        {
            if (entry.RatePermille <= 0) continue;
            if (Random.Shared.Next(10_000) >= entry.RatePermille) continue;
            var dur = entry.DurationMs > 0 ? (int)entry.DurationMs : 10_000;
            _sc.Start(target, entry.Sc, val1: 1, val2: 0, val3: 0, val4: 0, dur, attacker);
            _logger.LogDebug(
                "battle_autocast_aftercast: PC {Char} procced {Sc} on target {Tgt} (rate {Rate}/10000)",
                attacker.CharacterId, entry.Sc, target.Id.Value, entry.RatePermille);
        }
    }

    public void AutocastElemBuff(PlayerEntity attacker, ushort skillId)
    {
        // Wave 65 — rAthena battle_autocast_elembuff_skill (battle.cpp:6685).
        // OnSkill autobonus rows fire when the PC casts skillId. The
        // skill-id filter currently lives inside the bonus row's flag
        // column; PlayerBonusService rolls every OnSkill entry on each
        // call (a future refinement threads skillId so per-row gating
        // can fire).
        _bonusSvc?.ExecuteAutobonus(attacker, AutobonusTrigger.OnSkill);
    }

    public void VanishDamage(PlayerEntity attacker, Entity target, int hpPercent, int spPercent)
    {
        var (hp, maxHp) = HpOf(target);
        var (sp, maxSp) = SpOf(target);
        var hpLoss = (int)((long)maxHp * Math.Clamp(hpPercent, 0, 100) / 100);
        var spLoss = (int)((long)maxSp * Math.Clamp(spPercent, 0, 100) / 100);
        if (hpLoss > 0) _damage.ApplyDamage(target, Math.Min(hpLoss, hp), attacker);
        if (spLoss > 0)
        {
            switch (target)
            {
                case PlayerEntity p: p.Sp = Math.Max(0, p.Sp - spLoss); break;
                case MobEntity m: m.Sp = Math.Max(0, m.Sp - spLoss); break;
            }
        }
        _logger.LogDebug(
            "battle_vanish_damage: {Src} drained {Tgt} -{Hp}HP -{Sp}SP",
            attacker.CharacterId, target.Id.Value, hpLoss, spLoss);
    }

    public long VellumDamage(Entity target, int percentOfMaxHp)
    {
        var (_, maxHp) = HpOf(target);
        return (long)maxHp * Math.Clamp(percentOfMaxHp, 0, 100) / 100;
    }

    public bool StatusBlocksDamage(Entity target, BattleAttackType type)
    {
        // SC_STEELBODY / SC_GVG_GIANT / SC_INVINCIBLE / SC_BERSERK
        // are the rAthena candidates. None registered yet — service
        // exists so the resolver checks one canonical place.
        return false;
    }

    private static (int Hp, int MaxHp) HpOf(Entity e) => e switch
    {
        PlayerEntity p => (p.Hp, p.MaxHp),
        MobEntity m    => (m.Hp, m.MaxHp),
        _ => (0, 1),
    };

    private static (int Sp, int MaxSp) SpOf(Entity e) => e switch
    {
        PlayerEntity p => (p.Sp, p.MaxSp),
        MobEntity m    => (m.Sp, m.Stats.MaxSp),
        _ => (0, 1),
    };
}
