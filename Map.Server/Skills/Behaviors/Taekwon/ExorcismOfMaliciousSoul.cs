using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SOA_EXORCISM_OF_MALICIOUS_SOUL — auto-generated stub from
/// <c>src/map/skills/taekwon/exorcismofmalicioussoul.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ExorcismOfMaliciousSoul : RecursiveDamageSplashSkillImpl
{
    public ExorcismOfMaliciousSoul() : base(SkillIds.SOA_EXORCISM_OF_MALICIOUS_SOUL) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const status_change *tsc = status_get_sc(target);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 150 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SOA_SOUL_MASTERY) * 2;
    // 	skillratio += 1 * sstatus->spl;
    // 
    // 	if ((tsc != nullptr && tsc->getSCE(SC_SOULCURSE) != nullptr) || (sc != nullptr && sc->getSCE(SC_TOTEM_OF_TUTELARY) != nullptr))
    // 		skillratio += 100 * skill_lv;
    // 
    // 	if (sd != nullptr)
    // 		skillratio *= sd->soulball_old;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( sd != nullptr ){
    // 		// Remove old souls if any exist.
    // 		sd->soulball_old = sd->soulball;
    // 		pc_delsoulball( *sd, sd->soulball, 0 );
    // 	}
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }
}
