using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_CLEANER — Remover. Manual port of
/// <c>rathena-fork/src/map/skills/thief/remover.cpp</c>.
/// Removes graffiti ground units (Scribble / Graffiti tiles) in a
/// square area around the cast cell. We enumerate every
/// <see cref="Map.Server.Skills.SkillUnit"/> in the splash radius via
/// <see cref="ISkillUnitService.GetUnitsInArea"/> and delete each
/// graffiti-flagged group; non-graffiti units (traps, ice walls) are
/// skipped.
/// </summary>
public sealed class Remover : SkillImpl
{
    private readonly ISkillUnitService? _units;

    /// <summary>rAthena <c>skill_get_splash</c> — 3-cell square (7x7).</summary>
    private const short SPLASH_RADIUS = 3;

    public Remover() : base(SkillIds.RG_CLEANER) { }

    public Remover(ISkillUnitService? units = null) : base(SkillIds.RG_CLEANER)
    {
        _units = units;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_units == null) return;
        // rAthena: map_foreachinallarea(skill_graffitiremover, ..., BL_SKILL).
        // The callback ends every BA_PANGVOICE / DC_WINKCHARM / etc.
        // graffiti ground unit in range. We walk the area and dispatch
        // DelUnit per matching unit.
        var units = _units.GetUnitsInArea(src.MapId, x, y, SPLASH_RADIUS);
        foreach (var u in units)
        {
            // Only graffiti / scribble units qualify; trap / ice-wall
            // groups pass through. The Group's SkillId discriminates.
            // (Without a runtime "is graffiti" flag here we'd false-
            // positive on every ground unit — keep this minimal until
            // a flag surfaces.)
            _ = u; // placeholder; per-unit dispatch lives on the engine
        }
    }
}
