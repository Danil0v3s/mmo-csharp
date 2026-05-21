using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_MANDRAGORA — Splash SC_HOWLINGMANDRAGORA application.</summary>
public sealed class NpcHowlingOfMandragora : SkillImpl
{
    public NpcHowlingOfMandragora() : base(SkillIds.NPC_MANDRAGORA) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Mandragora, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
