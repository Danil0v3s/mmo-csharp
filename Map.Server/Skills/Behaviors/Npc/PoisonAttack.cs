using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_POISON — Weapon hit; 20*lv % SC_POISON; +20% hit rate.</summary>
public sealed class PoisonAttack : WeaponSkillImpl
{
    public PoisonAttack() : base(SkillIds.NPC_POISON) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 20 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 20 / 100);
}
