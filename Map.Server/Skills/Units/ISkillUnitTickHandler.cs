using Map.Server.Entities;

namespace Map.Server.Skills.Units;

/// <summary>
/// One-per-skill plugin describing how a ground-placed skill unit
/// behaves over its lifetime. Mirrors the rAthena <c>skill_unit_*</c>
/// callback family in <c>src/map/skill.cpp</c>:
/// <list type="bullet">
///   <item><c>skill_unit_onplace</c> — entity stepped onto a unit cell;</item>
///   <item><c>skill_unit_onplace_timer</c> / periodic tick — recurring effect;</item>
///   <item><c>skill_unit_onout</c> / <c>skill_unit_onleft</c> — entity left the cell;</item>
///   <item><c>skill_unit_ondamaged</c> — the unit itself absorbed damage (Ice Wall HP, trap detonate).</item>
/// </list>
///
/// <para>Handlers are registered through DI as <see cref="ISkillUnitTickHandler"/>
/// and indexed by <see cref="SkillId"/> in <see cref="SkillUnitTickRegistry"/>.
/// <see cref="SkillUnitService"/> dispatches to the matching handler
/// every tick — moving the per-skill switch out of the service and
/// into testable plugin classes.</para>
///
/// <para>Default implementations on the optional hooks are no-ops so a
/// pure damage-only unit (Magnus, Storm Gust) just overrides
/// <see cref="DurationMs"/> / <see cref="IntervalMs"/> / <see cref="Radius"/>
/// / <see cref="OnTick"/>.</para>
/// </summary>
public interface ISkillUnitTickHandler
{
    /// <summary>rAthena skill id this handler binds to.</summary>
    ushort SkillId { get; }

    /// <summary>Total group lifetime in ms (rAthena <c>skill_get_time</c>).</summary>
    int DurationMs(ushort skillLevel);

    /// <summary>Tick cadence in ms (rAthena <c>skill_get_time2</c> for most ground units).</summary>
    int IntervalMs(ushort skillLevel);

    /// <summary>
    /// Square half-side around the cast center; total cells covered
    /// = (2*radius + 1)². Most rAthena units use a per-skill layout
    /// matrix; this is the rectangular fallback. Set radius=0 for a
    /// single-cell unit (Pneuma, Safety Wall).
    /// </summary>
    int Radius(ushort skillLevel);

    /// <summary>
    /// Periodic effect — fired once per <see cref="IntervalMs"/> for every
    /// valid victim standing on the cell.
    /// </summary>
    void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx);

    /// <summary>
    /// rAthena <c>skill_unit_onplace</c> — fired the instant an entity
    /// steps onto the cell. Use for one-shot SC application (Pneuma's
    /// <c>SC_PNEUMA</c>, Sanctuary's heal-then-end). Default = no-op.
    /// </summary>
    void OnPlace(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }

    /// <summary>
    /// rAthena <c>skill_unit_onleft</c> / <c>skill_unit_onout</c> — fired
    /// when an entity moves off the cell. Use for SC removal that
    /// shouldn't persist beyond the unit (Bragi's song, Lullaby).
    /// Default = no-op.
    /// </summary>
    void OnLeft(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }

    /// <summary>
    /// rAthena <c>skill_unit_ondamaged</c> — the unit took external
    /// damage (Ice Wall HP, trap detonation). Default = no-op.
    /// </summary>
    void OnDamaged(SkillUnit unit, long damage, ISkillUnitContext ctx) { }

    /// <summary>
    /// Per-handler victim filter. Default: alive + not the caster + on
    /// the correct map (caller has already established cell-coincidence).
    /// Override for friendly-only units (Sanctuary), self-only units, etc.
    /// </summary>
    bool IsValidVictim(Entity? caster, Entity victim)
    {
        if (caster != null && victim.Id == caster.Id) return false;
        return victim switch
        {
            PlayerEntity p => p.Hp > 0,
            MobEntity m => m.Hp > 0,
            _ => false,
        };
    }
}
