using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// SA_LANDPROTECTOR — Sage Land Protector / Magnetic Earth. An immobile, no-damage
/// ground unit that suppresses other ground-skill placement on its cells (the gate
/// lives in <see cref="SkillUnitService.Place"/> via <c>CellHasLandProtector</c>).
/// It applies no SC and deals no damage — its only job is to occupy cells, so every
/// lifecycle hook is a no-op.
///
/// <para>rAthena (db/re/skill_db.yml SA_LANDPROTECTOR): Duration1 165s→345s
/// (<c>120000 + 45000*lv</c>); Unit Layout Size 3/3/4/4/5 → a 3×3 (lv1-2), 5×5
/// (lv3-4), 7×7 (lv5) square, i.e. radius 1/1/2/2/3; Interval -1 (no tick).</para>
/// </summary>
public sealed class LandProtectorUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.SA_LANDPROTECTOR;

    public int DurationMs(ushort skillLevel) => 120_000 + 45_000 * skillLevel;

    // Interval -1 in rAthena: the unit never ticks. A positive cadence keeps the
    // sweep loop cheap; OnTick does nothing regardless.
    public int IntervalMs(ushort skillLevel) => 1_000;

    public int Radius(ushort skillLevel) => skillLevel <= 2 ? 1 : skillLevel <= 4 ? 2 : 3;

    // No periodic effect — Land Protector neither damages nor buffs.
    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }

    // The unit affects no entity directly, so nobody is ever a "victim".
    public bool IsValidVictim(Entity? caster, Entity victim) => false;
}
