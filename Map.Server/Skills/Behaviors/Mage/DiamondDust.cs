using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_DIAMONDDUST — auto-generated stub from
/// <c>src/map/skills/mage/diamonddust.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DiamondDust : SkillImpl
{
    public DiamondDust() : base(SkillIds.SO_DIAMONDDUST) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* sc = status_get_sc(src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	int32 rate = 5 + 5 * skill_lv;
    // 
    // 	if( sc && sc->getSCE(SC_COOLER_OPTION) )
    // 		rate += (sd ? sd->status.job_level / 5 : 0);
    // 
    // 	sc_start(src, target, SC_CRYSTALIZE, rate, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 2 * sstatus->int_ + 300 * pc_checkskill(sd, SA_FROSTWEAPON) + sstatus->int_ * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	if( sc && sc->getSCE(SC_COOLER_OPTION) )
    // 		skillratio += (sd ? sd->status.job_level * 5 : 0);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag|=1; // Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
