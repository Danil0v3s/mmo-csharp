using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_METEORASSAULT — Meteor Assault. Manual port of
/// <c>rathena-fork/src/map/skills/thief/meteorassault.cpp</c>.
/// Recursive splash; renewal ratio <c>+100 + 120*lv</c>. On hit
/// applies one of Blind / Stun / Bleeding at <c>5 + 5*lv</c>%.
/// </summary>
public sealed class MeteorAssault : RecursiveDamageSplashSkillImpl
{
    public MeteorAssault() : base(SkillIds.ASC_METEORASSAULT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 + 120 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var chance = 5 + skillLevel * 5;
        if (System.Random.Shared.Next(100) >= chance) return;
        var sc = System.Random.Shared.Next(3) switch
        {
            0 => StatusType.Blind,
            1 => StatusType.Stun,
            _ => StatusType.Bleeding
        };
        ctx.Sc?.Start(target, sc, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
