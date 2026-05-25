using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleConfigService"/>. In-memory knob map
/// pre-populated with the rAthena defaults, then **overlaid from
/// JSON files** under <c>Map.Server/config/battle/*.json</c>. Those
/// files are produced by <c>Tools.RathenaImporter --conf-only</c>
/// from rAthena's <c>conf/battle/*.conf</c> and each has a sibling
/// <c>*.schema.json</c> so editors give autocomplete + docs from
/// rAthena's own inline comments.
///
/// rAthena ships ~600 knobs spread across conf/battle/*.conf. The
/// in-memory defaults below cover the subset the C# port currently
/// reads; the JSON overlay loads everything so a knob added by a
/// future port lands automatically as soon as <see cref="GetValue"/>
/// is called.
/// </summary>
public sealed class BattleConfigService : IBattleConfigService
{
    private readonly ConcurrentDictionary<string, int> _knobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<BattleConfigService> _logger;

    public BattleConfigService(ILogger<BattleConfigService> logger)
    {
        _logger = logger;
        SetDefaults();
        LoadJsonOverlay();
    }

    /// <summary>
    /// Overlay all JSON files under <c>config/battle/</c> onto the
    /// in-memory knob map. Each file is a flat <c>{name: value}</c>
    /// map (plus an optional <c>$schema</c> key). Booleans collapse
    /// to 0/1 to match the existing int-only knob storage.
    /// </summary>
    private void LoadJsonOverlay()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "config", "battle");
        if (!Directory.Exists(dir))
        {
            // Try the source-tree location for in-development runs.
            dir = Path.Combine(Directory.GetCurrentDirectory(), "config", "battle");
            if (!Directory.Exists(dir))
            {
                _logger.LogDebug("battle JSON overlay: directory not found ({Dir})", dir);
                return;
            }
        }
        var loaded = 0;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            if (file.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("$schema")) continue;
                    var v = prop.Value;
                    int intVal = v.ValueKind switch
                    {
                        JsonValueKind.Number => v.TryGetInt32(out var i) ? i : (int)v.GetInt64(),
                        JsonValueKind.True => 1,
                        JsonValueKind.False => 0,
                        JsonValueKind.String => int.TryParse(v.GetString(), out var s) ? s : 0,
                        _ => 0,
                    };
                    if (v.ValueKind != JsonValueKind.Object)
                    {
                        _knobs[prop.Name] = intVal;
                        loaded++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "battle JSON overlay: failed to read {File}", file);
            }
        }
        _logger.LogInformation("battle_config: loaded {N} knobs from JSON overlay ({Dir})", loaded, dir);
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
        _knobs["major_overweight_rate"] = 90;
        _knobs["item_first_get_time"] = 3000;
        _knobs["item_second_get_time"] = 1000;
        _knobs["item_third_get_time"] = 1000;
        _knobs["mvp_item_first_get_time"] = 10000;
        _knobs["mvp_item_second_get_time"] = 10000;
        _knobs["mvp_item_third_get_time"] = 2000;
        _knobs["max_parameter"] = 99;
        _knobs["max_third_parameter"] = 130;
        _knobs["max_baselevel"] = 175;

        // Cast timing knobs consumed by ISkillCastTimingService.
        // rAthena defaults: castrate_dex_scale=150, cast_rate=100,
        // delay_rate=100, delay_dependon_dex=0, delay_dependon_agi=0,
        // min_skill_delay_limit=100, no_skill_delay=BL_MOB (=2),
        // default_fixed_castrate=20.
        _knobs["castrate_dex_scale"] = 150;
        _knobs["cast_rate"] = 100;
        _knobs["delay_rate"] = 100;
        _knobs["delay_dependon_dex"] = 0;
        _knobs["delay_dependon_agi"] = 0;
        _knobs["min_skill_delay_limit"] = 100;
        _knobs["no_skill_delay"] = 2;
        _knobs["default_fixed_castrate"] = 20;
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
