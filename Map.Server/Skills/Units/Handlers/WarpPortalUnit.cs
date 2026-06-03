using Map.Server.Entities;
using Map.Server.Movement;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// AL_WARP — Acolyte Warp Portal. A single-cell ground unit placed at the cast cell whose
/// exit is the destination chosen from the chooser (stored on the group's
/// <see cref="SkillUnitGroup.DestMap"/>/<c>DestX</c>/<c>DestY</c> by the AL_WARP selection
/// path). Any player who steps on the portal is warped there — including the caster, and
/// any other player who walks onto it (rAthena <c>UNT_WARP_ACTIVE</c> in
/// <c>skill_unit_onplace</c>). Mobs are not warped.
///
/// <para>rAthena (db/re/skill_db.yml AL_WARP): Duration1 10s→25s (<c>5000 + 5000*lv</c>);
/// Unit Interval -1 (no tick); single cell (radius 0).</para>
/// </summary>
public sealed class WarpPortalUnit : ISkillUnitTickHandler
{
    private readonly IPcSetposService? _setpos;

    public WarpPortalUnit() { }
    public WarpPortalUnit(IPcSetposService? setpos) => _setpos = setpos;

    public ushort SkillId => SkillIds.AL_WARP;

    public int DurationMs(ushort skillLevel) => 5_000 + 5_000 * skillLevel;
    public int IntervalMs(ushort skillLevel) => 1_000; // Interval -1 in rAthena; the cadence is inert
    public int Radius(ushort skillLevel) => 0;          // single cell at the cast position

    // Warp the stepper to the portal's stored exit. Only players warp.
    public void OnPlace(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx, SkillUnitGroup group)
    {
        if (victim is not PlayerEntity pc) return;
        if (string.IsNullOrEmpty(group.DestMap)) return;
        _setpos?.Setpos(pc, group.DestMap, group.DestX, group.DestY);
    }

    // No periodic effect — the warp is an on-step event, not a tick.
    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }

    // Every alive player on the cell is warped — including the caster (Warp Portal does not
    // exclude its owner). Mobs are filtered out (only PCs warp).
    public bool IsValidVictim(Entity? caster, Entity victim) => victim is PlayerEntity { Hp: > 0 };
}
