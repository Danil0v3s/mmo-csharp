using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_SPLASHER — auto-generated stub from
/// <c>src/map/skills/thief/venomsplasher.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class VenomSplasher : RecursiveDamageSplashSkillImpl
{
    public VenomSplasher() : base(SkillIds.AS_SPLASHER) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start2(src, target, SC_POISON, 100, skill_lv, src->id, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // #ifdef RENEWAL
    // 	base_skillratio += -100 + 400 + 100 * skill_lv;
    // #else
    // 	base_skillratio += 400 + 50 * skill_lv;
    // #endif
    // 	if(sd)
    // 		base_skillratio += 20 * pc_checkskill(sd,AS_POISONREACT);
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( status_has_mode(tstatus,MD_STATUSIMMUNE)
    // 	// Renewal dropped the 3/4 hp requirement
    // #ifndef RENEWAL
    // 		|| tstatus-> hp > tstatus->max_hp*3/4
    // #endif
    // 			) {
    // 		if (sd) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		}
    // 		return;
    // 	}
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv,
    // 		sc_start4(src,target,type,100,skill_lv,getSkillId(),src->id,skill_get_time(getSkillId(),skill_lv),1000));
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // SkillImplRecursiveDamageSplash::castendDamageId(src, target, skill_lv, tick, flag);
    // 
    // 	if (!(flag & 1)) {
    // 		// Don't consume a second gemstone.
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 	}
    }
}
