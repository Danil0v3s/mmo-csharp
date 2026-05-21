using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_HEILIGE_PFERD — Homunculus Heilige Pferd. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_heiligepferd.cpp</c>.
/// Ratio <c>+(-100 + 1200 + 350*lv*BaseLv/100) + VIT</c>.
/// </summary>
public sealed class HeiligePferd : RecursiveDamageSplashSkillImpl
{
    public HeiligePferd() : base(SkillIds.MH_HEILIGE_PFERD) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1200 + 350 * skillLevel * src.Level / 100) + src.Stats.Vit;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
