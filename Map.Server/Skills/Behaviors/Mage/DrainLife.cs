using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_DRAINLIFE — auto-generated stub from
/// <c>src/map/skills/mage/drainlife.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DrainLife : SkillImpl
{
    public DrainLife() : base(SkillIds.WL_DRAINLIFE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 200 * skill_lv + sstatus->int_;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 heal = (int32)skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	int32 rate = 70 + 5 * skill_lv;
    // 
    // 	heal = heal * (5 + 5 * skill_lv) / 100;
    // 
    // 	if( target->type == BL_SKILL )
    // 		heal = 0; // Don't absorb heal from Ice Walls or other skill units.
    // 
    // 	if( heal && rnd()%100 < rate )
    // 	{
    // 		status_heal(src, heal, 0, 0);
    // 		clif_skill_nodamage(nullptr, *src, AL_HEAL, heal);
    // 	}
    }
}
