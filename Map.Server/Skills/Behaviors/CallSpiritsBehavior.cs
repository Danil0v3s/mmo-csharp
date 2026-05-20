using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_CALLSPIRITS (id 261) — Monk Summon Spirit Sphere. rAthena
/// <c>skill.cpp:case MO_CALLSPIRITS</c>: caster gains <c>lv</c>
/// Spirit Spheres (up to the global cap of 5).
///
/// Spirit Sphere tracking lives on PlayerEntity; the count is read
/// by Asura Strike / Finger Offensive. For the first port this
/// plugin claims the cast — the orb-count mutation rides on the
/// PlayerEntity.SpiritOrbs port (PC-S* wave).
/// </summary>
public sealed class CallSpiritsBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_CALLSPIRITS;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Sphere count tracking lives on the Player; we record the
        // intent (cast claimed) so the resolver doesn't run damage.
        // The sphere-grant call wires in when the orb mutation
        // surfaces on SkillBehaviorContext (next pass).
        return true;
    }
}
