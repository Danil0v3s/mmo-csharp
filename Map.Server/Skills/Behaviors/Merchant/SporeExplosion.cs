using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_SPORE_EXPLOSION — Genetic Spore Explosion. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/sporeexplosion.cpp</c>.
/// Ratio: <c>+(-100 + 400 + 200*lv)</c>. On-hit SC_SPORE_EXPLOSION.
/// </summary>
public sealed class SporeExplosion : RecursiveDamageSplashSkillImpl
{
    public SporeExplosion() : base(SkillIds.GN_SPORE_EXPLOSION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 200 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.SporeExplosion, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
}
