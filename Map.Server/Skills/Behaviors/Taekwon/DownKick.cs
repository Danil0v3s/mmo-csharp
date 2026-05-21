using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_DOWNKICK — Heel Drop. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/downkick.cpp</c>.
/// +60 + 20*lv ratio; 33.33% chance to stun.
/// </summary>
public sealed class DownKick : WeaponSkillImpl
{
    public DownKick() : base(SkillIds.TK_DOWNKICK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 60 + 20 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(10000) < 3333)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
