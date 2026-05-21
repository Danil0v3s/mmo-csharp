using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDEWEB — 100% SC_WIDEWEB (slowdown) on splash hit.</summary>
public sealed class WideWeb : SkillImpl
{
    public WideWeb() : base(SkillIds.NPC_WIDEWEB) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Wideweb, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 20_000, src);
    }
}
