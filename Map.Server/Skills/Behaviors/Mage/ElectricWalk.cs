using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_ELECTRICWALK — auto-generated stub from
/// <c>src/map/skills/mage/electricwalk.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ElectricWalk : SkillImpl
{
    public ElectricWalk() : base(SkillIds.SO_ELECTRICWALK) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* sc = status_get_sc(src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if (sc && sc->getSCE(type))
    // 		status_change_end(src, type);
    // 
    // 	sc_start2(src, src, type, 100, getSkillId(), skill_lv, skill_get_time(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 
    // 	skillratio += -100 + 60 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	if( sc && sc->getSCE(SC_BLAST_OPTION) )
    // 		skillratio += (sd ? sd->status.job_level / 2 : 0);
    return baseRatio;
    }
}
