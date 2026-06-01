using System.Linq;
using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Skills.Splash;

/// <summary>
/// SKILL-03 — the single allegiance resolver shared by the splash victim
/// filter (<see cref="MapForeachInRangeService"/>) and the damage gate
/// (<c>DamageService.CanDamage</c>) so the two can never disagree. Port of
/// rAthena <c>battle_check_target</c> (battle.cpp): slave-mob master
/// substitution + PvP/GvG/BG mapflag-driven friendly-fire.
/// </summary>
public static class BattleTargetResolver
{
    /// <summary>
    /// Allegiance of <paramref name="target"/> relative to <paramref name="src"/>.
    /// Resolves a summoned slave to its master (one hop) before deciding.
    /// </summary>
    public static BattleCheckTarget Classify(
        Entity? src,
        Entity target,
        IEntityRegistry entities,
        IMapFlagService? flags,
        IMapWorldRegistry? world)
    {
        if (src == null) return BattleCheckTarget.Enemy; // null src → everyone is a candidate
        if (src.Id == target.Id) return BattleCheckTarget.Self;

        // rAthena master substitution: a slave mob is evaluated from its
        // master's perspective. Resolve one hop for both sides.
        var cs = Controller(src, entities);
        var ct = Controller(target, entities);

        // Same controller → master vs its own slave, or two sibling slaves → friendly.
        if (cs.Id == ct.Id) return BattleCheckTarget.Party;

        if (cs is PlayerEntity sp && ct is PlayerEntity tp)
            return ClassifyPlayers(sp, tp, flags, world);
        if (cs is PlayerEntity && ct is MobEntity) return BattleCheckTarget.Enemy;
        if (cs is MobEntity && ct is PlayerEntity) return BattleCheckTarget.Enemy;
        // Mob ↔ mob (different masters / both wild) — not mutually hostile.
        return BattleCheckTarget.Neutral;
    }

    /// <summary>The entity that "owns" combat allegiance for <paramref name="e"/>:
    /// a summoned slave's master (one hop), else itself.</summary>
    private static Entity Controller(Entity e, IEntityRegistry entities)
    {
        if (e is MobEntity m && m.MasterId is { } mid && mid.Value != 0 && mid.Value != e.Id.Value)
        {
            var master = entities.Get(mid);
            if (master != null) return master; // single hop only — no cycles
        }
        return e;
    }

    private static BattleCheckTarget ClassifyPlayers(
        PlayerEntity sp, PlayerEntity tp, IMapFlagService? flags, IMapWorldRegistry? world)
    {
        bool sameParty = sp.PartyId != 0 && sp.PartyId == tp.PartyId;
        bool sameGuild = sp.GuildId != 0 && sp.GuildId == tp.GuildId;

        var name = MapName(sp.MapId, world);
        bool pvp = name != null && flags?.IsSet(name, MapFlag.Pvp) == true;
        bool gvg = name != null && flags?.IsSet(name, MapFlag.Gvg) == true;
        bool bg = name != null && flags?.IsSet(name, MapFlag.Battleground) == true;

        // Field map (no hostile zone): affiliations stay friendly (so buffs
        // resolve), but two unaffiliated players are NOT mutually attackable.
        if (!pvp && !gvg && !bg)
        {
            if (sameParty) return BattleCheckTarget.Party;
            if (sameGuild) return BattleCheckTarget.Guild;
            return BattleCheckTarget.Neutral;
        }

        // Hostile zone. Same party/guild suppresses enemy status UNLESS a
        // friendly-fire mapflag (WoE / PvP no-party-protection) re-enables it.
        if (sameParty)
        {
            bool ff = (pvp && flags!.IsSet(name!, MapFlag.PvpNoparty))
                   || (gvg && flags!.IsSet(name!, MapFlag.GvgNoparty));
            if (!ff) return BattleCheckTarget.Party;
        }
        if (sameGuild)
        {
            // GvG guildmates are always allies (WoE guild teams); PvP honors pvp_noguild.
            bool ff = pvp && flags!.IsSet(name!, MapFlag.PvpNoguild);
            if (!ff) return BattleCheckTarget.Guild;
        }
        return BattleCheckTarget.Enemy;
    }

    private static string? MapName(uint mapId, IMapWorldRegistry? world)
        => world?.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == mapId)?.Name;
}
