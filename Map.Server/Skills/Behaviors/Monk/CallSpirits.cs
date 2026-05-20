using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_CALLSPIRITS — Monk Summon Spirit Sphere. Mirrors
/// <c>rathena-fork/src/map/skills/monk/callspirits.cpp</c>.
///
/// Caster gains lv Spirit Spheres (cap 5). Sphere tracking lives
/// on PlayerEntity (port pending in PC-S* wave).
/// </summary>
public sealed class CallSpirits : SkillImpl
{
    public CallSpirits() : base(SkillIds.MO_CALLSPIRITS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Sphere count mutation lands when the orb hook surfaces.
    }
}
