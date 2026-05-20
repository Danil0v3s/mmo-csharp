using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SL_STIN — auto-generated stub from
/// <c>src/map/skills/taekwon/estin.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Estin : SkillImpl
{
    public Estin() : base(SkillIds.SL_STIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	// Target size must be small (0) for full damage
    // 	base_skillratio += (tstatus->size != SZ_SMALL ? -99 : 10 * skill_lv);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if (sd && !battle_config.allow_es_magic_pc && target->type != BL_MOB) {
    // 		status_change_start(src,src,SC_STUN,10000,skill_lv,0,0,0,500,SCSTART_NOTICKDEF|SCSTART_NORATEDEF);
    // 		clif_skill_fail( *sd, getSkillId() );
    // 		return;
    // 	}
    // 	skill_attack(BF_MAGIC,src,src,target,getSkillId(),skill_lv,tick,flag);
    }
}
