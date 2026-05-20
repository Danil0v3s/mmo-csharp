using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SKE_MIDNIGHT_KICK — auto-generated stub from
/// <c>src/map/skills/taekwon/midnightkick.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MidnightKick : RecursiveDamageSplashSkillImpl
{
    public MidnightKick() : base(SkillIds.SKE_MIDNIGHT_KICK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 800 + 1500 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SKE_SKY_MASTERY) * 5 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 
    // 	if (sc != nullptr && (sc->getSCE(SC_MIDNIGHT_MOON) != nullptr || sc->getSCE(SC_SKY_ENCHANT) != nullptr)) {
    // 		skillratio += 950 + 250 * skill_lv;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
