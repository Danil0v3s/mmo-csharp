using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_MILLENNIUMSHIELD — Self SC_MILLENNIUMSHIELD buff (block damage).</summary>
public sealed class MilleniumShield2 : SkillImpl
{
    public MilleniumShield2() : base(SkillIds.NPC_MILLENNIUMSHIELD) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.Millenniumshield, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
