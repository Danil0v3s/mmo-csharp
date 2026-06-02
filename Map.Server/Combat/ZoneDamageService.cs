using System.Linq;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.World;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IZoneDamageService"/>. Resolves the source map's GvG /
/// Battleground flag and applies the matching <c>battle_config</c> rate per
/// rAthena <c>battle_calc_gvg_damage</c> / <c>battle_calc_bg_damage</c>
/// (battle.cpp:2121 / 2046). Skills use the per-lane rate; normal attacks use
/// the short/long range rate. Rates come from <see cref="IBattleConfigService"/>
/// (knob names match rAthena's <c>battle_config</c>), defaulting to the
/// out-of-the-box values when a knob is unset.
///
/// COMBAT-62 adds the two remaining renewal gates that share this post-damage
/// stage: the <c>INF2_IGNOREGVGREDUCTION</c>/<c>INF2_IGNOREBGREDUCTION</c> bypass
/// (a flagged skill skips zone scaling) and the PK damage rate
/// (<c>battle_calc_pk_damage</c>, battle.cpp:2158 — PC↔PC under <c>pk_mode</c>,
/// applied independently of the GvG/BG zone).
/// </summary>
public sealed class ZoneDamageService : IZoneDamageService
{
    // rAthena battle.cpp battle_data defaults (lines 11650-11654 / 11866-11870).
    private const int DefaultLaneRate = 60;   // weapon / magic / misc
    private const int DefaultRangeRate = 80;  // short / long
    // PK range defaults differ from gvg/bg: pk_short 80, pk_long 70 (battle.cpp:11656-11657).
    private const int DefaultPkShortRate = 80;
    private const int DefaultPkLongRate = 70;

    private readonly IMapFlagService _flags;
    private readonly IMapWorldRegistry _maps;
    private readonly IBattleConfigService? _config;
    private readonly ISkillDb? _skills;

    public ZoneDamageService(IMapFlagService flags, IMapWorldRegistry maps,
        IBattleConfigService? config = null, ISkillDb? skills = null)
    {
        _flags = flags;
        _maps = maps;
        _config = config;
        _skills = skills;
    }

    public long Scale(BattleAttackType lane, Entity src, Entity target, long damage,
        bool isSkill, bool isShortRange, ushort skillId)
    {
        if (damage <= 0) return damage;

        // --- GvG / BG zone scaling (battle_calc_gvg/bg_damage) ---
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == src.MapId);
        string? zone = null;
        if (map != null)
        {
            if (_flags.IsSet(map.Name, MapFlag.Gvg)) zone = "gvg";
            else if (_flags.IsSet(map.Name, MapFlag.Battleground)) zone = "bg";
        }

        if (zone != null)
        {
            // COMBAT-62 — INF2_IGNOREGVGREDUCTION / INF2_IGNOREBGREDUCTION: the skill is
            // exempt from zone reduction entirely (battle.cpp:2060 / 2150 return damage).
            var ignoreFlag = zone == "gvg" ? SkillInf2.IgnoreGvgReduction : SkillInf2.IgnoreBgReduction;
            bool ignore = skillId != 0 && _skills != null && _skills.GetInf2(skillId, ignoreFlag);
            if (!ignore)
            {
                int rate = isSkill
                    ? LaneRate(zone, lane)
                    : Rate($"{zone}_{(isShortRange ? "short" : "long")}_attack_damage_rate", DefaultRangeRate);
                damage = damage * rate / 100;
                if (damage < 1) damage = 1; // rAthena i64max(damage, 1)
            }
        }

        // --- PK damage rate (battle_calc_pk_damage, battle.cpp:2158) ---
        damage = ApplyPkRate(lane, src, target, damage, isSkill, isShortRange);
        return damage;
    }

    /// <summary>
    /// COMBAT-62 — <c>battle_calc_pk_damage</c> (battle.cpp:2158): when <c>pk_mode</c>
    /// is enabled and both source and target are players, scale by the PK lane rate
    /// (skills) or the short/long rate (normal attacks). Independent of the GvG/BG
    /// zone — rAthena calls it from <c>battle_calc_damage</c>, so it stacks on top of
    /// any zone reduction already applied above.
    /// </summary>
    private long ApplyPkRate(BattleAttackType lane, Entity src, Entity target, long damage,
        bool isSkill, bool isShortRange)
    {
        if (damage <= 0) return damage;
        if (_config == null || _config.GetValue("pk_mode") == 0) return damage;
        if (src is not PlayerEntity || target is not PlayerEntity) return damage;

        int rate = isSkill
            ? LaneRate("pk", lane) // pk_{weapon|magic|misc}_attack_damage_rate, default 60
            : Rate($"pk_{(isShortRange ? "short" : "long")}_attack_damage_rate",
                   isShortRange ? DefaultPkShortRate : DefaultPkLongRate);

        damage = damage * rate / 100;
        return damage < 1 ? 1 : damage; // rAthena i64max(damage, 1)
    }

    private int LaneRate(string zone, BattleAttackType lane)
    {
        var laneName = lane switch
        {
            BattleAttackType.Magic => "magic",
            BattleAttackType.Misc => "misc",
            _ => "weapon",
        };
        return Rate($"{zone}_{laneName}_attack_damage_rate", DefaultLaneRate);
    }

    private int Rate(string knob, int fallback)
    {
        if (_config == null) return fallback;
        var v = _config.GetValue(knob);
        return v != 0 ? v : fallback;
    }
}
