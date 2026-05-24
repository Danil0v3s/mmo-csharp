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

    public SkillCastEndService(
        ISkillDb db,
        SkillResolverRegistry resolvers,
        ISkillUnitService units,
        ILogger<SkillCastEndService> logger)
    {
        _db = db;
        _resolvers = resolvers;
        _units = units;
        _logger = logger;
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
        // rAthena routes Teleport / Greed / SaveOption here. The
        // PlayerEntity warp helper lives in `IPlayerWarpService` once
        // it ports; until then the entry point is documented as
        // deferred per PARITY-REMAINING.md §P2.2 so callers know where to plug in.
        _logger.LogDebug(
            "skill_castend_map: {Skill} → {Map} (warp deferred per PARITY-REMAINING.md §P2.2)",
            skillId, targetMap);
        return false;
    }
}
