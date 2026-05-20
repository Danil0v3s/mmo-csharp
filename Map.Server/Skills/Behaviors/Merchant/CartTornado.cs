using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CART_TORNADO — auto-generated stub from
/// <c>src/map/skills/merchant/carttornado.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CartTornado : RecursiveDamageSplashSkillImpl
{
    public CartTornado() : base(SkillIds.GN_CART_TORNADO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	// ATK [( Skill Level x 200 ) + ( Cart Weight / ( 150 - Caster Base STR ))] + ( Cart Remodeling Skill Level x 50 )] %
    // 	base_skillratio += -100 + 200 * skill_lv;
    // 	if(sd && sd->cart_weight)
    // 		base_skillratio += sd->cart_weight / 10 / (150 - min(sd->status.str,120)) + pc_checkskill(sd,GN_REMODELING_CART) * 50;
    // 	if (sc && sc->getSCE(SC_BIONIC_WOODENWARRIOR))
    // 		base_skillratio *= 2;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, src, getSkillId(), skill_lv, tick, flag);
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
