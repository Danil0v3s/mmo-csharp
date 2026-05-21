using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CURSEATTACK — Weapon hit; 20*lv % SC_CURSE.</summary>
public sealed class CurseAttack : WeaponSkillImpl
{
    public CurseAttack() : base(SkillIds.NPC_CURSEATTACK) { }
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 20 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
