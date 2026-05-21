using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_VULCANARM — Mechanic Vulcan Arm. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/vulcanarm.cpp</c>.
/// Ratio <c>+(-100 + 230*lv) + DEX</c>. If the caster has SC_ABR_DUAL_CANNON
/// the attack divides into two hits.
/// </summary>
public sealed class VulcanArm : RecursiveDamageSplashSkillImpl
{
    public VulcanArm() : base(SkillIds.NC_VULCANARM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 230 * skillLevel) + src.Stats.Dex;

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // SC_ABR_DUAL_CANNON splits Vulcan Arm into two hits.
        // Sc availability isn't passed through ModifyDamageData yet — TODO once
        // the SC service is plumbed into the damage hook. For now we leave Hits=1.
    }
}
