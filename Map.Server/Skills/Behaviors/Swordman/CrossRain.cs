using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_CROSS_RAIN — auto-generated stub from
/// <c>src/map/skills/swordman/crossrain.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CrossRain : SkillImpl
{
    public CrossRain() : base(SkillIds.IG_CROSS_RAIN) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag|=1;//Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change* sc = status_get_sc(src);
    // 
    // 	if( sc && sc->getSCE( SC_HOLY_S ) ){
    // 		skillratio += -100 + ( 650 + 15 * pc_checkskill( sd, IG_SPEAR_SWORD_M ) ) * skill_lv;
    // 	}else{
    // 		skillratio += -100 + ( 450 + 10 * pc_checkskill( sd, IG_SPEAR_SWORD_M ) ) * skill_lv;
    // 	}
    // 	skillratio += 7 * sstatus->spl;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
