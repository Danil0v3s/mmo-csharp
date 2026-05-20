using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_WILD_WALK — auto-generated stub from
/// <c>src/map/skills/archer/wildwalk.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WildWalk : WeaponSkillImpl
{
    public WildWalk() : base(SkillIds.WH_WILD_WALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 1800 + 2800 * skill_lv;
    // 	// !TODO: unknown con and WH_NATUREFRIENDLY/HT_STEELCROW skills ratio
    // 	skillratio += 5 * sstatus->con;
    // 	skillratio += skillratio * pc_checkskill(sd, WH_NATUREFRIENDLY) / 10;
    // 	skillratio += skillratio * pc_checkskill(sd, HT_STEELCROW) / 10;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	sc_start(src, src, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(),skill_lv));
    }
}
