using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_BLEEDING — Weapon hit; 20*lv % SC_BLEEDING.</summary>
public sealed class Bleeding : WeaponSkillImpl
{
    public Bleeding() : base(SkillIds.NPC_BLEEDING) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 20 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
