using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DARKCROSS — Ratio +35*lv; 3*lv % SC_BLIND.</summary>
public sealed class CrossOfDarkness : WeaponSkillImpl
{
    public CrossOfDarkness() : base(SkillIds.NPC_DARKCROSS) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 35 * skillLevel;
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
