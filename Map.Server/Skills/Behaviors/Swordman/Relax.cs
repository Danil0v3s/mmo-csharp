using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LK_TENSIONRELAX — Lord Knight Tension Relax. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/relax.cpp</c>.
/// Applies SC_TENSIONRELAX with val3 = skill_get_time2 (HP recovery
/// tick interval). Time2 lookup is TODO.
/// </summary>
public sealed class Relax : SkillImpl
{
    public Relax() : base(SkillIds.LK_TENSIONRELAX) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Tensionrelax, val1: skillLevel, 0, 0, val4: 10_000, durationMs: 600_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
