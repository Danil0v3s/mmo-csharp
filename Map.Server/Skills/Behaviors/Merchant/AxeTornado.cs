using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_AXETORNADO — Mechanic Axe Tornado. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/axetornado.cpp</c>.
/// Ratio: <c>+(-100 + 200 + 180*lv) + 2*VIT</c>; SC_AXE_STOMP adds
/// +380 (caster SC TODO).
/// </summary>
public sealed class AxeTornado : RecursiveDamageSplashSkillImpl
{
    public AxeTornado() : base(SkillIds.NC_AXETORNADO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 + 180 * skillLevel) + src.Stats.Vit * 2;
}
