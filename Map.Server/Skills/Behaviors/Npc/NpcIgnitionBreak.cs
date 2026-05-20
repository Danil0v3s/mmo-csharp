using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_IGNITIONBREAK — auto-generated stub from
/// <c>src/map/skills/npc/npcignitionbreak.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcIgnitionBreak : RecursiveDamageSplashSkillImpl
{
    public NpcIgnitionBreak() : base(SkillIds.NPC_IGNITIONBREAK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // 3x3 cell Damage   = 1000  1500  2000  2500  3000 %
    // 	// 7x7 cell Damage   = 750   1250  1750  2250  2750 %
    // 	// 11x11 cell Damage = 500   1000  1500  2000  2500 %
    // 	int32 i = distance_bl(src,target);
    // 	if (i < 2)
    // 		base_skillratio += -100 + 500 * (skill_lv + 1);
    // 	else if (i < 4)
    // 		base_skillratio += -100 + 250 + 500 * skill_lv;
    // 	else
    // 		base_skillratio += -100 + 500 * skill_lv;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = 0;
    // #if PACKETVER >= 20180207
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // #else
    // 	clif_skill_damage( *src, *src, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // #endif
    // 	map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR|BL_SKILL, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|SD_SPLASH|1, skill_castend_damage_id);
    }
}
