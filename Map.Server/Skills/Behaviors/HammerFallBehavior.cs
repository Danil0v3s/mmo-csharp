using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// BS_HAMMERFALL (id 110) — Blacksmith Hammer Fall. rAthena
/// <c>skill.cpp:case BS_HAMMERFALL</c>: physical hit + stun chance
/// <c>20 + 10 * lv</c>%. Stun duration 2000 + 200*lv ms.
///
/// Damage flows through the generic Weapon resolver (skill_db
/// DamageRate handles the scaling); plugin layers the stun proc.
/// </summary>
public sealed class HammerFallBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.BS_HAMMERFALL;

    private readonly Random _rng;
    public HammerFallBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null)
        {
            var chance = 20 + 10 * skillLevel;
            if (_rng.Next(100) < chance)
            {
                var stunMs = 2_000 + 200 * skillLevel;
                ctx.Sc.Start(target, StatusType.Stun, val1: 1, 0, 0, 0, stunMs, source);
            }
        }
        // Fall through to generic Weapon resolver for the actual hit.
        return false;
    }
}
