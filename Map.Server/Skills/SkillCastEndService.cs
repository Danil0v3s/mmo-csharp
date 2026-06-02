using Map.Server.Entities;
using Map.Server.Skills.Resolvers;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillCastEndService"/>. Routes the four
/// rAthena castend entry points (`damage_id`, `nodamage_id`, `pos2`,
/// `map`) to either the existing
/// <see cref="SkillResolverRegistry"/> (for offensive / support
/// skills) or <see cref="ISkillUnitService"/> (for ground-target
/// skills). The map-warp branch is deferred per PARITY-REMAINING.md §P2.2 — a
/// `pc_setpos`-style warp helper that takes a coordinate from the
/// menu selection.
/// </summary>
public sealed class SkillCastEndService : ISkillCastEndService
{
    private readonly ISkillDb _db;
    private readonly SkillResolverRegistry _resolvers;
    private readonly ISkillUnitService _units;
    private readonly ILogger<SkillCastEndService> _logger;
    // COMBAT-26 — CastEndMap warp surface (Teleport random / save-point).
    private readonly Map.Server.Movement.IPlayerPositionHelpers? _positions;
    private readonly Map.Server.Combat.IPcDeathService? _death;
    private readonly Map.Server.World.IMapFlagService? _mapFlags;
    private readonly Map.Server.World.IMapWorldRegistry? _maps;
    // COMBAT-48 — AL_WARP memo-destination warp (pc_setpos).
    private readonly Map.Server.Movement.IPcSetposService? _setpos;

    public SkillCastEndService(
        ISkillDb db,
        SkillResolverRegistry resolvers,
        ISkillUnitService units,
        ILogger<SkillCastEndService> logger,
        Map.Server.Movement.IPlayerPositionHelpers? positions = null,
        Map.Server.Combat.IPcDeathService? death = null,
        Map.Server.World.IMapFlagService? mapFlags = null,
        Map.Server.World.IMapWorldRegistry? maps = null,
        Map.Server.Movement.IPcSetposService? setpos = null)
    {
        _db = db;
        _resolvers = resolvers;
        _units = units;
        _logger = logger;
        _positions = positions;
        _death = death;
        _mapFlags = mapFlags;
        _maps = maps;
        _setpos = setpos;
    }

    public bool CastEndDamageId(Entity source, Entity target, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        // Damage-side resolvers cover Weapon / Magic / Misc.
        var resolver = _resolvers.Get(def.DamageKind);
        if (resolver == null) return false;
        resolver.Resolve(source, target, def, skillLevel);
        return true;
    }

    public bool CastEndNoDamageId(Entity source, Entity target, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        // Support-side resolvers cover Heal + None (status applies).
        var resolver = _resolvers.Get(def.DamageKind);
        if (resolver == null) return false;
        resolver.Resolve(source, target, def, skillLevel);
        return true;
    }

    public bool CastEndPos2(Entity source, short x, short y, ushort skillId, ushort skillLevel)
    {
        var group = _units.Place(source, skillId, skillLevel, x, y);
        if (group == null)
        {
            _logger.LogDebug(
                "skill_castend_pos2: no ground-unit spec for {Skill} (deferred per PARITY-REMAINING.md §P2.2)",
                skillId);
            return false;
        }
        return true;
    }

    public bool CastEndMap(Entity source, string targetMap, ushort skillId)
    {
        // COMBAT-26 — rAthena skill_castend_map. Only PCs warp.
        if (source is not PlayerEntity pc) return false;

        switch (skillId)
        {
            case SkillIds.AL_TELEPORT:
            {
                // noteleport maps refuse Teleport (rAthena pc.cpp:pc_randomwarp /
                // map noteleport gate). Resolve the caster's current map name.
                var curMap = _maps?.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == pc.MapId);
                if (curMap != null && _mapFlags?.IsSet(curMap.Name, Map.Server.World.MapFlag.NoTeleport) == true)
                    return false;

                // The Teleport chooser sends the literal tokens "Random" /
                // "SavePoint" in ZC_WARPLIST; the client echoes the pick here.
                if (string.Equals(targetMap, "Random", System.StringComparison.OrdinalIgnoreCase))
                    return _positions?.RandomWarp(pc) ?? false;
                if (string.Equals(targetMap, "SavePoint", System.StringComparison.OrdinalIgnoreCase))
                    return _death?.WarpToSavepoint(pc) ?? false;
                return false;
            }
            case SkillIds.AL_WARP:
            {
                // rAthena skill_castend_map AL_WARP (skill.cpp:15693). The chooser
                // dismiss button sends "cancel" → abort with no warp.
                if (string.Equals(targetMap, "cancel", System.StringComparison.OrdinalIgnoreCase))
                    return false;

                // Destination list is level-gated: index 0 = save_point (lv≥1),
                // index i = memo_point[i-1] (i < lv), capped at lv 4. lv comes
                // from the learned AL_WARP level (pc_checkskill).
                var lv = pc.LearnedSkills.GetValueOrDefault(SkillIds.AL_WARP);
                if (lv <= 0) return false;
                if (lv > 4) lv = 4;

                // Source-map gate: rAthena MF_NOWARP forbids warping OUT.
                var srcMap = _maps?.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == pc.MapId);
                if (srcMap != null && _mapFlags?.IsSet(srcMap.Name, Map.Server.World.MapFlag.NoWarp) == true)
                    return false;

                // First chooser entry is the save point (sentinel "SavePoint").
                if (string.Equals(targetMap, "SavePoint", System.StringComparison.OrdinalIgnoreCase))
                    return _death?.WarpToSavepoint(pc) ?? false;

                // Otherwise resolve the memo slot whose map matches the pick,
                // within the level gate (memo slot s is reachable at lv ≥ s+2).
                for (var s = 0; s < pc.MemoPoints.Length; s++)
                {
                    var memo = pc.MemoPoints[s];
                    if (string.IsNullOrEmpty(memo.MapName)) continue;
                    if (!string.Equals(memo.MapName, targetMap, System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (lv < s + 2)
                    {
                        // Picked a destination above the caster's skill level.
                        _logger.LogDebug("skill_castend_map AL_WARP: {Map} above lv {Lv} gate", targetMap, lv);
                        return false;
                    }

                    // Destination gate: rAthena MF_NOWARPTO forbids warping IN.
                    if (_mapFlags?.IsSet(memo.MapName, Map.Server.World.MapFlag.NoWarpTo) == true)
                        return false;

                    // NOTE: rAthena places a Warp Portal ground unit at the cast
                    // cell whose exit is (memo.X, memo.Y); this port warps the
                    // caster directly (pc_setpos). The portal-placement and the
                    // deferred-requirement-consume / cancel-refund semantics
                    // (SKILL_NOCONSUME_REQ) are tracked in COMBAT-67 — here the
                    // SP was already consumed at cast (StartCast).
                    var result = _setpos?.Setpos(pc, memo.MapName, memo.X, memo.Y)
                                 ?? Map.Server.Movement.SetposResult.UnknownMap;
                    return result == Map.Server.Movement.SetposResult.Ok;
                }

                // No matching memo slot for the picked map.
                return false;
            }
            default:
                _logger.LogDebug("skill_castend_map: {Skill} → {Map} not a warp skill", skillId, targetMap);
                return false;
        }
    }
}
