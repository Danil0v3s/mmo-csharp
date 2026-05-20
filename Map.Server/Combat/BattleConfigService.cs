using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleConfigService"/>. In-memory knob map
/// pre-populated with the rAthena defaults; the .conf parser hook
/// will overlay values on top once the loader lands. Until then
/// the typed properties surface the most-used defaults so consumer
/// code can avoid hard-coded literals.
///
/// rAthena ships ~600 knobs spread across conf/battle/*.conf. We
/// intentionally don't enumerate them all here — see
/// .agents/migrations/map/battle-parity.md B-L1 for the porting
/// plan as new knobs surface in their consumers.
/// </summary>
public sealed class BattleConfigService : IBattleConfigService
{
    private readonly ConcurrentDictionary<string, int> _knobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<BattleConfigService> _logger;

    public BattleConfigService(ILogger<BattleConfigService> logger)
    {
        _logger = logger;
        SetDefaults();
    }

    /// <summary>rAthena <c>battle_set_defaults</c> — load the canonical defaults.</summary>
    private void SetDefaults()
    {
        // The subset the C# port already references. Add knobs here
        // when their consumer ports.
        _knobs["invincible_time"] = 5000;
        _knobs["min_hitrate"] = 5;
        _knobs["max_hitrate"] = 100;
        _knobs["min_shop_buy"] = 1;
        _knobs["min_shop_sell"] = 0;
        _knobs["mob_rudeattacked_count"] = 2;
        _knobs["natural_heal_weight_rate"] = 50;
        _knobs["natural_heal_weight_rate_renewal"] = 70;
        _knobs["item_first_get_time"] = 3000;
        _knobs["item_second_get_time"] = 1000;
        _knobs["item_third_get_time"] = 1000;
        _knobs["mvp_item_first_get_time"] = 10000;
        _knobs["mvp_item_second_get_time"] = 10000;
        _knobs["mvp_item_third_get_time"] = 2000;
        _knobs["max_parameter"] = 99;
        _knobs["max_third_parameter"] = 130;
        _knobs["max_baselevel"] = 175;
    }

    public int GetValue(string knob)
        => _knobs.TryGetValue(knob, out var v) ? v : 0;

    public void SetValue(string knob, int value)
    {
        _knobs[knob] = value;
        _logger.LogInformation("battle_set_value: {Knob}={Value}", knob, value);
    }

    public int InvincibleTimeMs => GetValue("invincible_time");
    public int MinHitRate => GetValue("min_hitrate");
    public int MaxHitRate => GetValue("max_hitrate");
    public int MinShopBuy => GetValue("min_shop_buy");
    public int MinShopSell => GetValue("min_shop_sell");
}
