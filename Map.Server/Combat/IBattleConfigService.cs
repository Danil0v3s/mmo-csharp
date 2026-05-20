namespace Map.Server.Combat;

/// <summary>
/// rAthena <c>battle_config</c> (battle.hpp:60 — ~600 knobs).
/// Loaded from <c>conf/battle/*.conf</c> + the dot-include chain
/// at boot. Exposes get/set by name; subsystems that care about a
/// single knob can also pull the typed properties below.
///
/// rAthena reference points: <c>battle_get_value</c> (battle.cpp:1100),
/// <c>battle_set_value</c> (battle.cpp:1090), <c>battle_config_read</c>
/// (battle.cpp:11785), <c>battle_set_defaults</c> (battle.cpp:11400),
/// <c>battle_adjust_conf</c> (battle.cpp:12110).
///
/// The first slice exposes only the knobs the C# port already cares
/// about; new properties land alongside the consumer (e.g. when the
/// elemental damage path needs <c>attack_attr_none</c>, that knob
/// joins this interface).
/// </summary>
public interface IBattleConfigService
{
    int GetValue(string knob);
    void SetValue(string knob, int value);

    /// <summary>rAthena <c>battle_config.invincible_time</c> default 5000.</summary>
    int InvincibleTimeMs { get; }

    /// <summary>rAthena <c>battle_config.min_hitrate</c> default 5.</summary>
    int MinHitRate { get; }

    /// <summary>rAthena <c>battle_config.max_hitrate</c> default 100.</summary>
    int MaxHitRate { get; }

    /// <summary>rAthena <c>battle_config.min_shop_buy</c> default 1.</summary>
    int MinShopBuy { get; }
    /// <summary>rAthena <c>battle_config.min_shop_sell</c> default 0.</summary>
    int MinShopSell { get; }
}
