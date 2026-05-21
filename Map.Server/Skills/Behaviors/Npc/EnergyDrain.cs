using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ENERGYDRAIN — Mob SP drain. Drain mechanics TODO.</summary>
public sealed class EnergyDrain : SkillImpl
{
    public EnergyDrain() : base(SkillIds.NPC_ENERGYDRAIN) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
