using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_SHADOW_STAB — auto-generated stub from
/// <c>src/map/skills/thief/shadowstab.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ShadowStab : WeaponSkillImpl
{
    public ShadowStab() : base(SkillIds.SHC_SHADOW_STAB) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 550 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 
    // 	if (wd->miscflag & SKILL_ALTDMG_FLAG) {
    // 		skillratio += 100 * skill_lv + 2 * sstatus->pow;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 
    // 	if (sc && sc->getSCE(SC_CLOAKINGEXCEED))
    // 		flag |= SKILL_ALTDMG_FLAG;
    // 
    // 	status_change_end(src, SC_CLOAKING);
    // 	status_change_end(src, SC_CLOAKINGEXCEED);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }
}
