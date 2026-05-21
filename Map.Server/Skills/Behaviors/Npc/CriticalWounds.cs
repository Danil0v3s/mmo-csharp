using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CRITICALWOUND — Weapon hit; 100% SC_CRITICALWOUND.</summary>
public sealed class CriticalWounds : WeaponSkillImpl
{
    public CriticalWounds() : base(SkillIds.NPC_CRITICALWOUND) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Criticalwound, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
}
