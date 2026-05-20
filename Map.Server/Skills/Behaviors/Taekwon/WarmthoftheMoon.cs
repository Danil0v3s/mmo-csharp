using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SG_MOON_WARM — auto-generated stub from
/// <c>src/map/skills/taekwon/warmthofthemoon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WarmthoftheMoon : SkillImpl
{
    public WarmthoftheMoon() : base(SkillIds.SG_MOON_WARM) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // A random 0~3 knockback bonus is added to the base knockback
    // 	dmg.blewcount += rnd_value(0, 3);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	std::shared_ptr<s_skill_unit_group> sg;
    // 
    // 	skill_clear_unitgroup(src);
    // 	if ((sg = skill_unitsetting(src,getSkillId(),skill_lv,src->x,src->y,0)))
    // 		sc_start4(src,src,type,100,skill_lv,0,0,sg->group_id,skill_get_time(getSkillId(),skill_lv));
    // 	flag|=1;
    }
}
