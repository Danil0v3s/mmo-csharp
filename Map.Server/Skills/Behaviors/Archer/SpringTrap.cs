using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SPRINGTRAP — auto-generated stub from
/// <c>src/map/skills/archer/springtrap.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpringTrap : SkillImpl
{
    public SpringTrap() : base(SkillIds.HT_SPRINGTRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 
    // 	skill_unit *su=nullptr;
    // 	if((target->type==BL_SKILL) && (su=(skill_unit *)target) && (su->group) ){
    // 		switch(su->group->unit_id){
    // 			case UNT_ANKLESNARE:	// ankle snare
    // 				if (su->group->val2 != 0)
    // 					// if it is already trapping something don't spring it,
    // 					// remove trap should be used instead
    // 					break;
    // 				[[fallthrough]];
    // 			case UNT_BLASTMINE:
    // 			case UNT_SKIDTRAP:
    // 			case UNT_LANDMINE:
    // 			case UNT_SHOCKWAVE:
    // 			case UNT_SANDMAN:
    // 			case UNT_FLASHER:
    // 			case UNT_FREEZINGTRAP:
    // 			case UNT_CLAYMORETRAP:
    // 			case UNT_TALKIEBOX:
    // 				su->group->unit_id = UNT_USED_TRAPS;
    // 				clif_changetraplook(target, UNT_USED_TRAPS);
    // 				su->group->limit=DIFF_TICK(tick+1500,su->group->tick);
    // 				su->limit=DIFF_TICK(tick+1500,su->group->tick);
    // 		}
    // 	}
    }
}
