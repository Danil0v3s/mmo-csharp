using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_PROVIDENCE — auto-generated stub from
/// <c>src/map/skills/swordman/resistantsouls.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ResistantSouls : SkillImpl
{
    public ResistantSouls() : base(SkillIds.CR_PROVIDENCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if(sd && dstsd){ //Check they are not another crusader [Skotlex]
    // 		if ((dstsd->class_&MAPID_SECONDMASK) == MAPID_CRUSADER) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 	}
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv,
    // 		sc_start(src,target,skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv)));
    }
}
