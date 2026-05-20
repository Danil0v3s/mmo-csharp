using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_CLOUD_KILL — auto-generated stub from
/// <c>src/map/skills/mage/cloudkill.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CloudKill : SkillImpl
{
    public CloudKill() : base(SkillIds.SO_CLOUD_KILL) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 40 * skill_lv;
    // 	skillratio += sstatus->int_ * 3;
    // 	RE_LVL_DMOD(100);
    // 	if (sc) {
    // 		if (sc->getSCE(SC_CURSED_SOIL_OPTION))
    // 			skillratio += (sd ? sd->status.job_level : 0);
    // 
    // 		if (sc->getSCE(SC_DEEP_POISONING_OPTION))
    // 			skillratio += skillratio * 1500 / 100;
    // 	}
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag |= 4;
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
