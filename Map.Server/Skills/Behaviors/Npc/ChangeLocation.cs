using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_MOVE_COORDINATE — Mob warp. Position service TODO.</summary>
public sealed class ChangeLocation : SkillImpl
{
    public ChangeLocation() : base(SkillIds.NPC_MOVE_COORDINATE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
