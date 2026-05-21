using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_GRIMTOOTH — Grimtooth. Manual port of
/// <c>rathena-fork/src/map/skills/thief/grimtooth.cpp</c>.
/// Recursive splash; ratio <c>+20*lv</c>. Mob-only targets get
/// SC_QUAGMIRE.
/// </summary>
public sealed class Grimtooth : RecursiveDamageSplashSkillImpl
{
    public Grimtooth() : base(SkillIds.AS_GRIMTOOTH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity && (target.Stats.Mode & MobMode.StatusImmune) == 0)
            ctx.Sc?.Start(target, StatusType.Quagmire, val1: 0, 0, 0, 0, durationMs: 5_000, src);
    }
}
