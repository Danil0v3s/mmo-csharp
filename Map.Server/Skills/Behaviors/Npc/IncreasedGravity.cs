using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_GRADUAL_GRAVITY — Mirrors
/// <c>rathena-fork/src/map/skills/npc/increasedgravity.cpp</c>.
/// Single-target SC application (NOT a placed ground unit). Casts
/// <c>SC_GRAVITATION</c> on the target at 100% rate with no
/// resistance modifiers.
/// </summary>
public sealed class IncreasedGravity : SkillImpl
{
    public IncreasedGravity() : base(SkillIds.NPC_GRADUAL_GRAVITY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Gravitation, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
