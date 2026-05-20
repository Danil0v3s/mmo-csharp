using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_FROSTNOVA — auto-generated stub from
/// <c>src/map/skills/mage/frostnova.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FrostNova : SkillImpl
{
    public FrostNova() : base(SkillIds.WZ_FROSTNOVA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_area_temp[1] = 0;
    // 	map_foreachinshootrange(skill_attack_area, src,
    // 		skill_get_splash(getSkillId(), skill_lv), splash_target(src),
    // 		BF_MAGIC, src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	// In renewal the damage formula is identical to MG_FROSTDIVER
    // 	base_skillratio += 10 * skill_lv;
    // #else
    // 	base_skillratio += -100 + (100 + skill_lv * 10) * 2 / 3;
    // #endif
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	sc_start(src,target,SC_FREEZE,(sd!=nullptr)?skill_lv*5+33:skill_lv*3+35,skill_lv,skill_get_time2(getSkillId(), skill_lv));
    }
}
