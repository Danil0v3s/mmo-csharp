using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_JACKFROST — Warlock Jack Frost. Water-element splash. Ratio
/// branches on SC_MISTY_FROST: with frost <c>-100+1200+600*lv</c>,
/// without <c>-100+1000+300*lv</c>. SC reader in this hook
/// signature isn't surfaced — we land the no-misty path.
/// </summary>
public sealed class JackFrost : RecursiveDamageSplashSkillImpl
{
    public JackFrost() : base(SkillIds.WL_JACKFROST) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 300 * skillLevel);
}
