using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PETRIFYATTACK — Weapon hit; 20*lv % SC_STONEWAIT; +20% hit rate.</summary>
public sealed class PetrifyAttack : WeaponSkillImpl
{
    public PetrifyAttack() : base(SkillIds.NPC_PETRIFYATTACK) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 20 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stonewait, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 20 / 100);
}
