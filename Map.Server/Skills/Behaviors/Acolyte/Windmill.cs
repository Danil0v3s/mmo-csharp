using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_WINDMILL — auto-generated stub from
/// <c>src/map/skills/acolyte/windmill.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Windmill : RecursiveDamageSplashSkillImpl
{
    public Windmill() : base(SkillIds.SR_WINDMILL) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if( dstsd )
    // 		skill_addtimerskill(src,tick+status_get_amotion(src),target->id,0,0,getSkillId(),skill_lv,BF_WEAPON,0);
    // 	else if( dstmd )
    // 		sc_start(src,target, SC_STUN, 100, skill_lv, 1000 + 1000 * (rnd() %3));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	// ATK [(Caster Base Level + Caster DEX) x Caster Base Level / 100] %
    // 	skillratio += -100 + status_get_lv(src) + sstatus->dex;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, src, getSkillId(), skill_lv, tick, flag);
    }
}
