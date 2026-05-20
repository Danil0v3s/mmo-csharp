using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_ASTRAL_STRIKE — auto-generated stub from
/// <c>src/map/skills/mage/astralstrike.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AstralStrike : SkillImpl
{
    public AstralStrike() : base(SkillIds.AG_ASTRAL_STRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	skillratio += -100 + 300 + 1800 * skill_lv + 10 * sstatus->spl;
    // 	if (tstatus->race == RC_UNDEAD || tstatus->race == RC_DRAGON)
    // 		skillratio += 100 + 300 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	map_foreachinallarea(skill_area_sub, src->m, x-i, y-i, x+i, y+i, BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_damage_id);
    // 	flag |= 1;
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
    }


}
