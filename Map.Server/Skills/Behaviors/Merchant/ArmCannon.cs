using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_ARMSCANNON — Mechanic Arm Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/armcannon.cpp</c>.
/// Ratio: <c>+(-100 + 400 + 350*lv)</c>.
///
/// <para>SC_ABR_DUAL_CANNON doubles div_ — 🚩 INFRA-DEFERRED:
/// <see cref="ModifyDamageData"/> has no <c>SkillBehaviorContext</c>
/// parameter, so the SC readback can't be wired here. Reroute once
/// the hook signature is extended (mirrors the same blocker on
/// <see cref="VulcanArm"/> / <see cref="ExplosivePowder"/> /
/// <see cref="MightySmash"/>).</para>
/// </summary>
public sealed class ArmCannon : RecursiveDamageSplashSkillImpl
{
    public ArmCannon() : base(SkillIds.NC_ARMSCANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 350 * skillLevel);
}
