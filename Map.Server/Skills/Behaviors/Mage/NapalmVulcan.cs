using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>HW_NAPALMVULCAN — High Wizard Napalm Vulcan. Splash; ratio +(-100+70*lv); Curse proc at 5*lv %.</summary>
public sealed class NapalmVulcan : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;
    public NapalmVulcan() : base(SkillIds.HW_NAPALMVULCAN) => _rng = Random.Shared;
    public NapalmVulcan(Random? rng = null) : base(SkillIds.HW_NAPALMVULCAN) => _rng = rng ?? Random.Shared;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 70 * skillLevel);
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
