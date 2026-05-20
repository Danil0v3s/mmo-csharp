using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleReflectService"/>.
///
/// <para>Reflect-immunity statuses on the defender (White Imprison,
/// Dark Crow, Kyomu) and Hell's Plant on the attacker zero the
/// reflect (rAthena battle.cpp:10033). Short-range gating tracks
/// the BF_SHORT branch — long-range hits don't trigger most
/// reflects.</para>
///
/// <para>Damage application uses <see cref="IDamageService"/> via
/// lazy resolution to avoid a construction cycle through
/// BattleCalculator → BattleCardService → BattleReflectService →
/// DamageService → ...</para>
/// </summary>
public sealed class BattleReflectService : IBattleReflectService
{
    private readonly IServiceProvider _services;
    private IDamageService? _cachedDamage;
    private IDamageService Damage => _cachedDamage ??= _services.GetRequiredService<IDamageService>();
    private readonly IStatusChangeService? _sc;
    private readonly ILogger<BattleReflectService> _logger;

    public BattleReflectService(
        IServiceProvider services,
        ILogger<BattleReflectService> logger,
        IStatusChangeService? sc = null)
    {
        _services = services;
        _logger = logger;
        _sc = sc;
    }

    public long CalcReturnDamage(Entity defender, Entity attacker, long damage, bool isShortRange)
    {
        if (damage <= 0) return 0;
        // Defender-side reflect-immunity (rAthena battle.cpp:10033).
        // Once the SC_WHITE_IMPRISON / DARK_CROW / KYOMU entries land
        // in StatusType, drop them here to early-out.
        // For now there are no reflect-immune SCs ported; nothing to gate.

        // Short-range branch only — long-range reflect happens via
        // SC_REFLECTDAMAGE (Reflect Damage) which we haven't ported.
        if (!isShortRange) return 0;

        // SC_REFLECTSHIELD: 20% + (skill_lv-1)*10. Until the SC
        // entry ports, we expose the API but return 0 — the canonical
        // entry still routes calls through here.
        // Equip-bonus short_weapon_damage_return needs the equip
        // aggregator (BattleStats doesn't carry it yet). Same
        // pattern as CardFix.
        return 0;
    }

    public void DoReflect(Entity defender, Entity attacker, long rDamage)
    {
        if (rDamage <= 0) return;
        // Apply back to the attacker; rAthena uses battle_fix_damage
        // with delay 0. Source = defender so death attribution lands
        // on the right party.
        Damage.ApplyDamage(attacker, (int)Math.Min(rDamage, int.MaxValue), defender);
        _logger.LogDebug(
            "battle_do_reflect: {Atk} took {Dmg} reflect from {Def}",
            attacker.Id.Value, rDamage, defender.Id.Value);
    }
}
