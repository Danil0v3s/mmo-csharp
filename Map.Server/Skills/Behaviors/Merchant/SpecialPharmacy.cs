using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_S_PHARMACY — auto-generated stub from
/// <c>src/map/skills/merchant/specialpharmacy.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpecialPharmacy : SkillImpl
{
    public SpecialPharmacy() : base(SkillIds.GN_S_PHARMACY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd ) {
    // 		int32 qty = 1;
    // 		sd->skill_id_old = getSkillId();
    // 		sd->skill_lv_old = skill_lv;
    // 		clif_cooking_list( *sd, 29, getSkillId(), qty, 6 );
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	}
    }
}
