using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_SPRINKLESAND — Sand Attack. Manual port of
/// <c>rathena-fork/src/map/skills/thief/sandattack.cpp</c>.
/// +30 ratio; 20% (player) / 15% (mob) blind on hit.
/// </summary>
public sealed class SandAttack : WeaponSkillImpl
{
    public SandAttack() : base(SkillIds.TF_SPRINKLESAND) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var chance = src is PlayerEntity ? 20 : 15;
        if (System.Random.Shared.Next(100) < chance)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
