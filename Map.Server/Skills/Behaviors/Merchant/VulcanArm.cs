using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_VULCANARM — Mechanic Vulcan Arm. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/vulcanarm.cpp</c>.
/// Ratio <c>+(-100 + 230*lv) + DEX</c>.
///
/// <para>SC_ABR_DUAL_CANNON splits the hit into 2 div_ — 🚩 INFRA-DEFERRED:
/// <see cref="ModifyDamageData"/> has no <c>SkillBehaviorContext</c>
/// parameter, so the caster SC readback can't be wired here. Reroute
/// once the hook signature is extended (same blocker as
/// <see cref="ArmCannon"/> / <see cref="ExplosivePowder"/> /
/// <see cref="MightySmash"/>).</para>
/// </summary>
public sealed class VulcanArm : RecursiveDamageSplashSkillImpl
{
    public VulcanArm() : base(SkillIds.NC_VULCANARM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 230 * skillLevel) + src.Stats.Dex;
}
