using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_BODYRELOCATION — Monk Body Relocation. Mirrors
/// <c>rathena-fork/src/map/skills/monk/bodyrelocation.cpp</c>.
///
/// Instant-teleport to adjacent target cell. No damage. The actual
/// warp call lives on the movement service; plugin records the cast
/// claim until IMovementService.Teleport ports.
/// </summary>
public sealed class BodyRelocation : SkillImpl
{
    public BodyRelocation() : base(SkillIds.MO_BODYRELOCATION) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Teleport rides on the movement service — plugin owns the cast.
    }
}
