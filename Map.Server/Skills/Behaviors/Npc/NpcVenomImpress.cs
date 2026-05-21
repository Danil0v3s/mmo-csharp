using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_VENOMIMPRESS — Splash SC_VENOMIMPRESS (target poison-vulnerable).</summary>
public sealed class NpcVenomImpress : SkillImpl
{
    public NpcVenomImpress() : base(SkillIds.NPC_VENOMIMPRESS) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Venomimpress, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
