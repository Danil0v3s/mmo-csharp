using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_TOXIN_OF_MANDARA — Homunculus Toxin of Mandara. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_toxinofmandara.cpp</c>.
/// Ratio <c>+(-100 + 400 + 450*lv*BaseLv/100) + DEX</c>. 100%
/// SC_TOXIN_OF_MANDARA on hit.
/// </summary>
public sealed class ToxinOfMandara : RecursiveDamageSplashSkillImpl
{
    public ToxinOfMandara() : base(SkillIds.MH_TOXIN_OF_MANDARA) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 450 * skillLevel * src.Level / 100) + src.Stats.Dex;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.ToxinOfMandara, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
}
