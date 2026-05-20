using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// HW_GRAVITATION — auto-generated stub from
/// <c>src/map/skills/mage/gravitationfield.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GravitationField : SkillImpl
{
    public GravitationField() : base(SkillIds.HW_GRAVITATION) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifndef RENEWAL
    // 	// Gravitation can trigger physical autospells
    // 	attack_type |= BF_NORMAL;
    // 	attack_type |= BF_WEAPON;
    // #endif
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	skillratio += -100 + 100 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // #endif
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	flag|=1;//Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    // #else
    // 	std::shared_ptr<s_skill_unit_group> sg;
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if ((sg = skill_unitsetting(src,getSkillId(),skill_lv,x,y,0)))
    // 		sc_start4(src,src,type,100,skill_lv,0,BCT_SELF,sg->group_id,skill_get_time(getSkillId(),skill_lv));
    // 	flag|=1;
    // #endif
    }
}
