using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SKE_ALL_IN_THE_SKY — auto-generated stub from
/// <c>src/map/skills/taekwon/allinthesky.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AllInTheSky : SkillImpl
{
    public AllInTheSky() : base(SkillIds.SKE_ALL_IN_THE_SKY) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (target->type == BL_PC)
    // 		status_zap(target, 0, 0, status_get_ap(target));
    // 	if( unit_movepos( src, target->x, target->y, 2, true ) ){
    // 		clif_snap(src, src->x, src->y);
    // 	}
    // 	skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	base_skillratio += -100 + 250 + 1200 * skill_lv;
    // 	base_skillratio += 5 * sstatus->pow;
    return baseRatio;
    }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // switch (status_get_race(&target)) {
    // 		case RC_DEMIHUMAN:
    // 		case RC_DEMON:
    // 			dmg.div_ = 3;
    // 			break;
    // 	}
    }
}
