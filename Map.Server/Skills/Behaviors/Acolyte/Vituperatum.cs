using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_VITUPERATUM — auto-generated stub from
/// <c>src/map/skills/acolyte/vituperatum.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Vituperatum : StatusSkillImpl
{
    public Vituperatum() : base(SkillIds.AB_VITUPERATUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag&1)
    // 		StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    // 	else {
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    }
}
