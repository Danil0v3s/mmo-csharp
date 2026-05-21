using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_ARMSCANNON — Mechanic Arm Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/armcannon.cpp</c>.
/// Ratio: <c>+(-100 + 400 + 350*lv)</c>. SC_ABR_DUAL_CANNON doubles
/// div_ (TODO).
/// </summary>
public sealed class ArmCannon : RecursiveDamageSplashSkillImpl
{
    public ArmCannon() : base(SkillIds.NC_ARMSCANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 350 * skillLevel);
}
