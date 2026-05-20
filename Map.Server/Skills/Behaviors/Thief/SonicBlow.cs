using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_SONICBLOW — auto-generated stub from
/// <c>src/map/skills/thief/sonicblow.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SonicBlow : WeaponSkillImpl
{
    public SonicBlow() : base(SkillIds.AS_SONICBLOW) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 
    // 	if (!map_flag_gvg2(target->m) && !map_getmapflag(target->m, MF_BATTLEGROUND) && sc && sc->getSCE(SC_SPIRIT) && sc->getSCE(SC_SPIRIT)->val2 == SL_ASSASIN)
    // 		sc_start(src, target, SC_STUN, (4 * skill_lv + 20), skill_lv, skill_get_time2(getSkillId(), skill_lv)); //Link gives double stun chance outside GVG/BG
    // 	else
    // 		sc_start(src, target, SC_STUN, (2 * skill_lv + 10), skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	base_skillratio += 100 + 100 * skill_lv;
    // 	if (tstatus->hp < (tstatus->max_hp / 2))
    // 		base_skillratio += base_skillratio / 2;
    // #else
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	base_skillratio += 200 + 50 * skill_lv;
    // 	if (sd && pc_checkskill(sd, AS_SONICACCEL) > 0)
    // 		base_skillratio += base_skillratio / 10;
    // #endif
    return baseRatio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if(sd && pc_checkskill(sd,AS_SONICACCEL) > 0)
    // #ifdef RENEWAL
    // 		hit_rate += hit_rate * 90 / 100;
    // #else
    // 		hit_rate += hit_rate * 50 / 100;
    // #endif
    return hitRate;
    }
}
