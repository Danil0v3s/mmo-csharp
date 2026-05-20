using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CARTCANNON — auto-generated stub from
/// <c>src/map/skills/merchant/cartcannon.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CartCannon : RecursiveDamageSplashSkillImpl
{
    public CartCannon() : base(SkillIds.GN_CARTCANNON) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(&src);
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_BIONIC_WOODENWARRIOR))
    // 		dmg.div_ = 2;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + (250 + 20 * pc_checkskill(sd, GN_REMODELING_CART)) * skill_lv + 2 * sstatus->int_ / (6 - pc_checkskill(sd, GN_REMODELING_CART));
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd && pc_checkskill(sd, GN_REMODELING_CART))
    // 		hit_rate += pc_checkskill(sd, GN_REMODELING_CART) * 4;
    return hitRate;
    }
}
