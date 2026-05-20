using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_SCREAM — auto-generated stub from
/// <c>src/map/skills/archer/dazzler.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Dazzler : SkillImpl
{
    public Dazzler() : base(SkillIds.DC_SCREAM) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 rate = 150 + 50 * skill_lv + 100; // Aegis accuracy (1000 = 100%). DC_SCREAM has a 10% higher base chance than BA_FROSTJOKER
    // 	int32 duration = skill_get_time2(getSkillId(), skill_lv);
    // 	if (battle_check_target(src, target, BCT_PARTY) > 0) {
    // 		// TODO: check DC_SCREAM rate and duration.
    // 		// DC_SCREAM and BA_FROSTJOKER initially shared the same code but the original comment only applies to BA_FROSTJOKER :
    // 		// "On party members: Chance is divided by 4 and BA_FROSTJOKER duration is fixed to 15000ms"
    // 		rate /= 4;
    // 		duration = skill_get_time(getSkillId(), skill_lv);
    // 	}
    // 	status_change_start(src, target, skill_get_sc(getSkillId()), rate*10, skill_lv, 0, 0, 0, duration, SCSTART_NONE);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_addtimerskill(src,tick+3000,target->id,src->x,src->y,getSkillId(),skill_lv,0,flag);
    // 
    // 	if (md) {
    // 		// custom hack to make the mob display the skill, because these skills don't show the skill use text themselves
    // 		//NOTE: mobs don't have the sprite animation that is used when performing this skill (will cause glitches)
    // 		char temp[70];
    // 		snprintf(temp, sizeof(temp), "%s : %s !!",md->name,skill_get_desc(getSkillId()));
    // 		clif_disp_overhead(md,temp);
    // 	}
    }
}
