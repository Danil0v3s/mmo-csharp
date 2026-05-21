using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_ENERGY_CANNONADE — Meister Energy Cannonade. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/energycannonade.cpp</c>.
/// Ratio: <c>+(-100 + 250 + 750*lv) + 5*POW</c>.
/// </summary>
public sealed class EnergyCannonade : RecursiveDamageSplashSkillImpl
{
    public EnergyCannonade() : base(SkillIds.MT_ENERGY_CANNONADE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 750 * skillLevel) + 5 * src.Stats.Pow;
}
