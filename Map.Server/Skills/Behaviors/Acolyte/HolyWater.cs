using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HOLYWATER — auto-generated stub from
/// <c>src/map/skills/acolyte/holywater.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HolyWater : SkillImpl
{
    public HolyWater() : base(SkillIds.AL_HOLYWATER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd)
    // 	{
    // 		if (skill_produce_mix(sd, getSkillId(), ITEMID_HOLY_WATER, 0, 0, 0, 1, -1))
    // 		{
    // 			if (skill_unit* su = map_find_skill_unit_oncell(bl, bl->x, bl->y, NJ_SUITON, nullptr, 0); su != nullptr)
    // 				skill_delunit(su);
    // 			clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
    // 		}
    // 		else
    // 			clif_skill_fail(*sd, getSkillId());
    // 	}
    }
}
