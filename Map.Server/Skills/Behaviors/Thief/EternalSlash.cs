using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_ETERNAL_SLASH — auto-generated stub from
/// <c>src/map/skills/thief/eternalslash.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EternalSlash : WeaponSkillImpl
{
    public EternalSlash() : base(SkillIds.SHC_ETERNAL_SLASH) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(&src);
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_E_SLASH_COUNT))
    // 		dmg.div_ = sc->getSCE(SC_E_SLASH_COUNT)->val1;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += -100 + 300 * skill_lv + 2 * sstatus->pow;
    // 
    // 	if( sc != nullptr && sc->getSCE( SC_SHADOW_EXCEED ) ){
    // 		skillratio += 120 * skill_lv + sstatus->pow;
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
    // 	if( sc && sc->getSCE(SC_E_SLASH_COUNT) )
    // 		sc_start(src, src, SC_E_SLASH_COUNT, 100, min( 5, 1 + sc->getSCE(SC_E_SLASH_COUNT)->val1 ), skill_get_time(getSkillId(), skill_lv));
    // 	else
    // 		sc_start(src, src, SC_E_SLASH_COUNT, 100, 1, skill_get_time(getSkillId(), skill_lv));
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }
}
