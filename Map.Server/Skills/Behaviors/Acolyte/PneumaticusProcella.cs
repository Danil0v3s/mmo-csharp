using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_PNEUMATICUS_PROCELLA — auto-generated stub from
/// <c>src/map/skills/acolyte/pneumaticusprocella.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PneumaticusProcella : SkillImpl
{
    public PneumaticusProcella() : base(SkillIds.CD_PNEUMATICUS_PROCELLA) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag|=1;
    // 
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	skillratio += -100 + 150 + 2100 * skill_lv + 10 * sstatus->spl;
    // 	skillratio += 3 * pc_checkskill( sd, CD_FIDUS_ANIMUS );
    // 	if (tstatus->race == RC_UNDEAD || tstatus->race == RC_DEMON) {
    // 		skillratio += 50 + 150 * skill_lv;
    // 		skillratio += 2 * pc_checkskill( sd, CD_FIDUS_ANIMUS );
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
