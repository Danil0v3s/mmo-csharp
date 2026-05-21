using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_POWERFUL_SWING — Meister Powerful Swing. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/powerfulswing.cpp</c>.
/// Ratio: <c>+(-100 + 300 + 850*lv) + 5*POW</c>. SC_AXE_STOMP bonus
/// TODO.
/// </summary>
public sealed class PowerfulSwing : RecursiveDamageSplashSkillImpl
{
    public PowerfulSwing() : base(SkillIds.MT_POWERFUL_SWING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 + 850 * skillLevel) + 5 * src.Stats.Pow;
}
