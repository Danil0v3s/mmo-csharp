using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillUsaveService"/>. Stores a single most-
/// recent cast per PC (rAthena tracks an array; first slice keeps the
/// last one). <see cref="UsaveTrigger"/> hands the saved cast back to
/// the cast-service caller.
/// </summary>
public sealed class SkillUsaveService : ISkillUsaveService
{
    private readonly Dictionary<EntityId, (ushort skill, ushort level)> _saved = new();
    private readonly ILogger<SkillUsaveService> _logger;

    public SkillUsaveService(ILogger<SkillUsaveService> logger) => _logger = logger;

    public void UsaveAdd(PlayerEntity caster, ushort skillId, ushort skillLevel)
        => _saved[caster.Id] = (skillId, skillLevel);

    public bool UsaveTrigger(PlayerEntity caster)
    {
        if (!_saved.TryGetValue(caster.Id, out var s)) return false;
        // The replay itself is the caller's responsibility — SC_DOUBLECAST
        // proc reads the saved row and re-issues the cast through the
        // SkillCastService. Entry point lands here so the read goes
        // through a typed call instead of poking the dictionary.
        return true;
    }
}

/// <summary>
/// Default <see cref="ISkillLayoutService"/>. Returns a square radius
/// fallback so existing callers (Storm Gust, Magnus Exorcismus) keep
/// working. The full rAthena layout matrix (firewall, icewall, lullaby,
/// wallofthorn lines) ports when skill_db's `Layout: ...` column is read.
/// </summary>
public sealed class SkillLayoutService : ISkillLayoutService
{
    public IReadOnlyList<(short dx, short dy)> GetLayout(int layoutType)
    {
        // First slice: single-cell layout (the SkillUnitService caller
        // expands to a square via spec radius). When the matrix YAML
        // loader lands, this method reads from it.
        return Array.Empty<(short, short)>();
    }
}
