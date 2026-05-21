using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_DESTRUCTIVE_HURRICANE — Arch Mage Destructive Hurricane. Manual port of
/// <c>rathena-fork/src/map/skills/mage/destructivehurricane.cpp</c>.
///
/// <para>Wind AOE splash. Ratio: <c>+(-100 + 600 + 2850*lv) + 5*SPL</c>.
/// SC_CLIMAX variants (val1=2 sets blewcount to 2; val1=1 fires an extra
/// AG_DESTRUCTIVE_HURRICANE_CLIMAX hit; val1=4 turns the spell into a
/// caster buff; val1=5 enlarges the splash to 19×19) are TODO until
/// caster SC introspection is wired here.</para>
/// </summary>
public sealed class DestructiveHurricane : RecursiveDamageSplashSkillImpl
{
    public DestructiveHurricane() : base(SkillIds.AG_DESTRUCTIVE_HURRICANE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 600 + 2850*lv + 5*SPL.
        return baseRatio + (-100 + 600 + 2850 * skillLevel) + 5 * src.Stats.Spl;
    }

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: dmg.blewcount = 2 when SC_CLIMAX val1 == 2 — caster SC readback TODO.
    }
}
