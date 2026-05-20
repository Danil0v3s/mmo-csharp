using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_TUNABELLY — auto-generated stub from
/// <c>src/map/skills/summoner/tunabelly.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TunaBelly : SkillImpl
{
    public TunaBelly() : base(SkillIds.SU_TUNABELLY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 
    // 	uint32 heal = 0;
    // 
    // 	if (dstmd && (dstmd->mob_id == MOBID_EMPERIUM || status_get_class_(target) == CLASS_BATTLEFIELD))
    // 		heal = 0;
    // 	else if (status_get_hp(target) != status_get_max_hp(target))
    // 		heal = ((2 * skill_lv - 1) * 10) * status_get_max_hp(target) / 100;
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	status_heal(target, heal, 0, 0);
    }
}
