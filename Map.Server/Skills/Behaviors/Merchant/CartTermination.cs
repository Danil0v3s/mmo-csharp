using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// WS_CARTTERMINATION — auto-generated stub from
/// <c>src/map/skills/merchant/carttermination.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CartTermination : WeaponSkillImpl
{
    public CartTermination() : base(SkillIds.WS_CARTTERMINATION) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_STUN,5*skill_lv,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	int32 i = 10 * (16 - skill_lv);
    // 	if (i < 1) i = 1;
    // 	//Preserve damage ratio when max cart weight is changed.
    // 	if (sd && sd->cart_weight)
    // 		base_skillratio += sd->cart_weight / i * 80000 / battle_config.max_cart_weight - 100;
    // 	else if (!sd)
    // 		base_skillratio += 80000 / i - 100;
    return baseRatio;
    }
}
