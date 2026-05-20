using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_ARMSCANNON — auto-generated stub from
/// <c>src/map/skills/merchant/armcannon.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ArmCannon : RecursiveDamageSplashSkillImpl
{
    public ArmCannon() : base(SkillIds.NC_ARMSCANNON) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(&src);
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_ABR_DUAL_CANNON))
    // 		dmg.div_ = 2;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += -100 + 400 + 350 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
