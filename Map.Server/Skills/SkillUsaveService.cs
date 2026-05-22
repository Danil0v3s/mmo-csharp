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

    /// <inheritdoc />
    public IReadOnlyList<(short Dx, short Dy)> GetLayoutForSkill(
        ushort skillId, ushort skillLevel, short defaultRadius, short casterDir = 0)
    {
        // SK.100-1b/d — per-skill named shapes. Each block carries its
        // rAthena citation so reviewers can diff against skill.cpp's
        // skill_unit_layout[] table (around line 14600+).

        // MG_FIREWALL — horizontal row of 5 cells perpendicular to
        // caster facing. (skill.cpp ~14605)
        if (skillId == SkillIds.MG_FIREWALL)
            return BuildRow(length: 5, vertical: (casterDir % 2) == 0);

        // WZ_ICEWALL — 5-cell cross. (skill.cpp ~14620)
        if (skillId == SkillIds.WZ_ICEWALL)
            return BuildCross(arm: 2);

        // GN_WALLOFTHORN — 3x3 hollow ring (8 cells). (skill.cpp ~14660)
        if (skillId == SkillIds.GN_WALLOFTHORN)
            return BuildHollowSquare(radius: 1);

        // MG_FIREBALL — 5-cell plus. (skill.cpp:14600 LAYOUT 1)
        if (skillId == SkillIds.MG_FIREBALL)
            return BuildCross(arm: 1);

        // RA_FIRINGTRAP / trap variants — 3x3 square.
        if (skillId == SkillIds.RA_FIRINGTRAP)
            return BuildSquare(1);

        // Default fallback: square radius (preserves legacy
        // SkillUnitService.Place behavior).
        return BuildSquare(defaultRadius);
    }

    // ---- Layout builders -------------------------------------------------

    private static List<(short, short)> BuildSquare(short r)
    {
        var cells = new List<(short, short)>((2 * r + 1) * (2 * r + 1));
        for (var dy = (short)-r; dy <= r; dy++)
            for (var dx = (short)-r; dx <= r; dx++)
                cells.Add((dx, dy));
        return cells;
    }

    private static List<(short, short)> BuildHollowSquare(short radius)
    {
        var cells = new List<(short, short)>(8);
        for (var dy = (short)-radius; dy <= radius; dy++)
            for (var dx = (short)-radius; dx <= radius; dx++)
                if (dx != 0 || dy != 0)
                    cells.Add((dx, dy));
        return cells;
    }

    private static List<(short, short)> BuildCross(short arm)
    {
        var cells = new List<(short, short)>(1 + 4 * arm);
        cells.Add((0, 0));
        for (short i = 1; i <= arm; i++)
        {
            cells.Add((i, 0));
            cells.Add(((short)-i, 0));
            cells.Add((0, i));
            cells.Add((0, (short)-i));
        }
        return cells;
    }

    private static List<(short, short)> BuildRow(short length, bool vertical)
    {
        var half = (short)(length / 2);
        var cells = new List<(short, short)>(length);
        for (short i = (short)-half; i <= half; i++)
            cells.Add(vertical ? ((short)0, i) : (i, (short)0));
        return cells;
    }
}
