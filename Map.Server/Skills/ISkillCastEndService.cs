using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Castend dispatchers. Canonical entry points for rAthena
/// <c>skill_castend_damage_id</c>, <c>skill_castend_nodamage_id</c>,
/// <c>skill_castend_pos2</c>, and <c>skill_castend_map</c>
/// (skill.cpp).
///
/// These four exist in rAthena as a switch tree on every skill id; in
/// the C# port the per-skill handler lives in an
/// <see cref="Resolvers.ISkillResolver"/> implementation, and these
/// methods route the cast result to the right resolver family.
///
/// Why expose them at all when the resolver registry already does the
/// job? Because every other rAthena caller (mob AI, autocast hooks,
/// status-change procs) reaches for a function named
/// <c>skill_castend_*</c>. Surfacing the entry point here means the
/// port reads like a 1:1 translation rather than each call site
/// rediscovering the resolver registry.
/// </summary>
public interface ISkillCastEndService
{
    /// <summary>rAthena <c>skill_castend_damage_id</c> — single-target damage cast.</summary>
    bool CastEndDamageId(Entity source, Entity target, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_castend_nodamage_id</c> — single-target support cast.</summary>
    bool CastEndNoDamageId(Entity source, Entity target, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_castend_pos2</c> — ground-target cast lands at (x, y).</summary>
    bool CastEndPos2(Entity source, short x, short y, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_castend_map</c> — map-warp / teleport cast.</summary>
    bool CastEndMap(Entity source, string targetMap, ushort skillId);
}
