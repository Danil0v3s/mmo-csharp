using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SR_CURSEDCIRCLE — Mob cursed circle target lock.</summary>
public sealed class NpcCursedCircle : SkillImpl
{
    public NpcCursedCircle() : base(SkillIds.NPC_SR_CURSEDCIRCLE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.CursedcircleTarget, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 15_000, src);
        ctx.Sc?.Start(src, StatusType.CursedcircleAtker, val1: skillLevel, 0, 0, 0, durationMs: 15_000, src);
    }
}
