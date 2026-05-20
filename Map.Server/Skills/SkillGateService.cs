using Map.Server.Entities;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillGateService"/>. Checks the noskill map
/// flag + the per-skill NoCastMask. Companion-type variants
/// (homun / merc) currently share the PC gate; the dedicated rules
/// land when those companions port their per-type skill_db tables.
/// </summary>
public sealed class SkillGateService : ISkillGateService
{
    private readonly ISkillDb _db;
    private readonly IMapFlagService _mapFlags;
    private readonly IMapWorldRegistry _maps;
    private readonly ILogger<SkillGateService> _logger;

    public SkillGateService(
        ISkillDb db,
        IMapFlagService mapFlags,
        IMapWorldRegistry maps,
        ILogger<SkillGateService> logger)
    {
        _db = db;
        _mapFlags = mapFlags;
        _maps = maps;
        _logger = logger;
    }

    public bool IsNotOk(PlayerEntity caster, ushort skillId)
    {
        var mapName = ResolveMapName(caster);
        if (mapName != null && _mapFlags.IsSet(mapName, MapFlag.NoSkill)) return true;

        // rAthena per-skill NoCastMask matches against the map's
        // zone-type bitfield. We don't have a zone-type column on the
        // map model yet — so we only honor the NoCastMask `0x1` bit
        // which means "every zone refuses this skill".
        var noCast = _db.GetNoCast(skillId);
        if ((noCast & 0x1) != 0) return true;

        return false;
    }

    public bool IsNotOkHom(Entity homun, ushort skillId)
    {
        // Homunculus skill list lives in homun_skill_tree; rAthena
        // gates it on the master's NoSkill flag. Same as PC for now.
        if (homun is PlayerEntity pc) return IsNotOk(pc, skillId);
        return false;
    }

    public bool IsNotOkMercenary(Entity merc, ushort skillId)
    {
        if (merc is PlayerEntity pc) return IsNotOk(pc, skillId);
        return false;
    }

    public bool IsNotOkNpcRange(Entity caster, ushort skillId, short x, short y)
    {
        // rAthena enforces: skill must be cast within 4 cells of the
        // NPC that triggered it. We don't track NPC-originated casts
        // on a per-target basis yet — the entry point stays canonical.
        return false;
    }

    public bool PosMaxcountCheck(Entity caster, ushort skillId, ushort skillLevel)
    {
        // rAthena caps simultaneous ground units per caster (e.g.
        // 1 Storm Gust, 3 traps). The cap lives on
        // SkillDefinition.MaxUnitCount; the existing
        // ISkillUnitService doesn't expose a per-caster count yet.
        // Entry point lands here so the gate runs; tightening to
        // an enforced cap is a follow-up when the unit registry
        // exposes the lookup.
        var cap = _db.GetMaxCount(skillId, skillLevel);
        if (cap <= 0) return true;
        return true;
    }

    private string? ResolveMapName(Entity e)
    {
        foreach (var m in _maps.All)
        {
            if ((uint)m.Name.GetHashCode() == e.MapId) return m.Name;
        }
        return null;
    }
}
