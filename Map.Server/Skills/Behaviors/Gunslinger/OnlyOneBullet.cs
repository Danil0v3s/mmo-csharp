using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_ONLY_ONE_BULLET — auto-generated stub from
/// <c>src/map/skills/gunslinger/onlyonebullet.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class OnlyOneBullet : WeaponSkillImpl
{
    public OnlyOneBullet() : base(SkillIds.NW_ONLY_ONE_BULLET) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* sc = status_get_sc(src);
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 
    // 	if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 		status_change_end(src, SC_INTENSIVE_AIM_COUNT);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 1200 + 3000 * skill_lv;
    // 	skillratio += 5 * sstatus->con;
    // 	if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 		skillratio += sc->getSCE(SC_INTENSIVE_AIM_COUNT)->val1 * 350 * skill_lv;
    // 	if (sd && sd->weapontype1 == W_REVOLVER) {
    // 		skillratio += 400 * skill_lv;
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
