using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_MIX_COOKING — auto-generated stub from
/// <c>src/map/skills/merchant/mixcooking.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MixCooking : SkillImpl
{
    public MixCooking() : base(SkillIds.GN_MIX_COOKING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd ) {
    // 		int32 qty = 1;
    // 		sd->skill_id_old = getSkillId();
    // 		sd->skill_lv_old = skill_lv;
    // 		if( skill_lv > 1 )
    // 			qty = 10;
    // 		clif_cooking_list( *sd, 27, getSkillId(), qty, 6 );
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	}
    }
}
