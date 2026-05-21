using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_BLEEDING2 — Weapon hit; (50 + 10*lv) % SC_BLEEDING.</summary>
public sealed class Bleeding2 : WeaponSkillImpl
{
    public Bleeding2() : base(SkillIds.NPC_BLEEDING2) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 50 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
