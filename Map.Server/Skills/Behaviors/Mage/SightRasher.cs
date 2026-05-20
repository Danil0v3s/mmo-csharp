using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_SIGHTRASHER — auto-generated stub from
/// <c>src/map/skills/mage/sightrasher.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SightRasher : SkillImpl
{
    public SightRasher() : base(SkillIds.WZ_SIGHTRASHER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Passive side of the attack.
    // 	status_change_end(src, SC_SIGHT);
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	map_foreachinshootrange(skill_area_sub,src,
    // 		skill_get_splash(getSkillId(), skill_lv),BL_CHAR|BL_SKILL,
    // 		src,getSkillId(),skill_lv,tick, flag|BCT_ENEMY|SD_ANIMATION|1,
    // 		skill_castend_damage_id);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC,src,src,target,getSkillId(),skill_lv,tick,flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 20 * skill_lv;
    return baseRatio;
    }
}
