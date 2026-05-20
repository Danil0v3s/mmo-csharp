using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_MALE — auto-generated stub from
/// <c>src/map/skills/other/iwillprotectyou.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class IWillProtectYou : SkillImpl
{
    public IWillProtectYou() : base(SkillIds.WE_MALE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 
    // 	uint8 hp_rate = abs(skill_get_hp_rate(getSkillId(), skill_lv));
    // 
    // 	if (hp_rate && status_get_hp(src) > status_get_max_hp(src) / hp_rate) {
    // 		int32 gain_hp = tstatus->max_hp * hp_rate / 100; // The earned is the same % of the target HP than it costed the caster. [Skotlex]
    // 
    // 		clif_skill_nodamage(src,*target,getSkillId(),status_heal(target, gain_hp, 0, 0));
    // 	}
    }
}
