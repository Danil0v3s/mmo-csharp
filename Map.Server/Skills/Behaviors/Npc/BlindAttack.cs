using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_BLINDATTACK — Weapon hit; 20*lv % SC_BLIND.</summary>
public sealed class BlindAttack : WeaponSkillImpl
{
    public BlindAttack() : base(SkillIds.NPC_BLINDATTACK) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 20 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
