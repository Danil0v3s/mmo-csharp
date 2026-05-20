using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AS_GRIMTOOTH (id 137) — Assassin Grimtooth. rAthena
/// <c>skill.cpp:case AS_GRIMTOOTH</c>: ranged katar attack at
/// (120 + 30 * lv)% ATK. Range increases per level — must be cast
/// while Hidden (rAthena gates at requirement check; consumes hiding).
///
/// First-port runs the damage; Hiding consumption is a side-effect
/// the cast-time pipeline owns and will plug in here when the
/// "consume SC on cast" hook lands. Single physical hit; defer
/// damage math to the generic resolver.
/// </summary>
public sealed class GrimToothBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AS_GRIMTOOTH;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Trigger generic Weapon resolver for damage; auto-consume
        // hiding so the caster pops out the moment Grimtooth resolves
        // (rAthena fold-in from skill_castend_damage_id).
        ctx.Sc?.End(source, Status.StatusType.Hiding);
        return false;
    }
}
