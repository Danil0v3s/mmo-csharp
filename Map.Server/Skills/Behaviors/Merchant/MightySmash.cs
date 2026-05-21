using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_MIGHTY_SMASH — Meister Mighty Smash. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/mightysmash.cpp</c>.
/// Ratio: <c>+(-100 + 80 + 240*lv) + 5*POW</c>. SC_AXE_STOMP bonus
/// +20 +5*POW TODO.
/// </summary>
public sealed class MightySmash : RecursiveDamageSplashSkillImpl
{
    public MightySmash() : base(SkillIds.MT_MIGHTY_SMASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 80 + 240 * skillLevel) + 5 * src.Stats.Pow;
}
