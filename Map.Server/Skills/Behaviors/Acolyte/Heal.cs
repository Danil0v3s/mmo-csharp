using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HEAL — auto-generated stub from
/// <c>src/map/skills/acolyte/heal.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Heal : SkillImpl
{
    public Heal() : base(SkillIds.AL_HEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(bl);
    // 	map_session_data *sd = BL_CAST(BL_PC, src);
    // 	map_session_data *dstsd = nullptr;
    // 	status_data* sstatus = status_get_status_data(*src);
    // 	mob_data *dstmd = BL_CAST(BL_MOB, bl);
    // 
    // 	int32 heal = skill_calc_heal(src, bl, getSkillId(), skill_lv, true);
    // 
    // 	if (status_isimmune(bl) || (dstmd && (status_get_class(bl) == MOBID_EMPERIUM || status_get_class_(bl) == CLASS_BATTLEFIELD)))
    // 		heal = 0;
    // 
    // 	if (tsc != nullptr && !tsc->empty())
    // 	{
    // 		if (tsc->getSCE(SC_KAITE) && !status_has_mode(sstatus, MD_STATUSIMMUNE))
    // 		{ // Bounce back heal
    // 			if (--tsc->getSCE(SC_KAITE)->val2 <= 0)
    // 				status_change_end(bl, SC_KAITE);
    // 			if (src == bl)
    // 				heal = 0; // When you try to heal yourself under Kaite, the heal is voided.
    // 			else
    // 			{
    // 				bl = src;
    // 				dstsd = sd;
    // 			}
    // 		}
    // 		else if (tsc->getSCE(SC_BERSERK) || tsc->getSCE(SC_SATURDAYNIGHTFEVER))
    // 		{
    // 			heal = 0; // Needed so that it actually displays 0 when healing.
    // 		}
    // 	}
    // 
    // 	status_change_end(bl, SC_BITESCAR);
    // 	clif_skill_nodamage(src, *bl, getSkillId(), heal);
    // 	if (tsc && tsc->getSCE(SC_AKAITSUKI) && heal)
    // 		heal = ~heal + 1;
    // 	t_exp heal_get_jobexp = status_heal(bl, heal, 0, 0);
    // 
    // 	if (sd && dstsd && heal > 0 && sd != dstsd && battle_config.heal_exp > 0)
    // 	{
    // 		heal_get_jobexp = heal_get_jobexp * battle_config.heal_exp / 100;
    // 		if (heal_get_jobexp <= 0)
    // 			heal_get_jobexp = 1;
    // 		pc_gainexp(sd, bl, 0, heal_get_jobexp, 0);
    // 	}
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }
}
