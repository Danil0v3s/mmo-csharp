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
    private readonly ILogger<BattleEffectsService> _logger;

    public BattleEffectsService(
        IDamageService damage,
        ILogger<BattleEffectsService> logger,
        IStatusChangeService? sc = null)
    {
        _damage = damage;
        _logger = logger;
        _sc = sc;
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
        // Bonus autospell aftercast — equip-bonus driven.
    }

    public void AutocastElemBuff(PlayerEntity attacker, ushort skillId)
    {
        // Element-buff autocast — same equip-bonus path.
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
