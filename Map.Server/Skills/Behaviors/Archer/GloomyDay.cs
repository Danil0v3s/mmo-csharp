using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_GLOOMYDAY — auto-generated stub from
/// <c>src/map/skills/archer/gloomyday.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GloomyDay : SkillImpl
{
    public GloomyDay() : base(SkillIds.WM_GLOOMYDAY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	if( dstsd && ( pc_checkskill(dstsd,KN_BRANDISHSPEAR) || pc_checkskill(dstsd,LK_SPIRALPIERCE) ||
    // 			pc_checkskill(dstsd,CR_SHIELDCHARGE) || pc_checkskill(dstsd,CR_SHIELDBOOMERANG) ||
    // 			pc_checkskill(dstsd,PA_SHIELDCHAIN) || pc_checkskill(dstsd,LG_SHIELDPRESS) ) )
    // 	{ // !TODO: Which skills aren't boosted anymore?
    // 		sc_start(src,target,SC_GLOOMYDAY_SK,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 		return;
    // 	}
    // 
    // 	sc_start(src,target,type,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    }
}
