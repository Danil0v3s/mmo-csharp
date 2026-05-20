using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_IMPERIAL_CROSS — auto-generated stub from
/// <c>src/map/skills/swordman/imperialcross.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ImperialCross : WeaponSkillImpl
{
    public ImperialCross() : base(SkillIds.IG_IMPERIAL_CROSS) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change* sc = status_get_sc(src);
    // 
    // 	skillratio += -100 + 1650 + 1350 * skill_lv;
    // 	skillratio += pc_checkskill(sd, IG_SPEAR_SWORD_M) * 25;
    // 	skillratio += 5 * sstatus->pow;	// !TODO: check POW ratio
    // 
    // 	if (sc != nullptr && sc->getSCE(SC_SPEAR_SCAR))
    // 		skillratio += 100 + 300 * skill_lv;
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
