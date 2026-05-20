using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_FEMALE — auto-generated stub from
/// <c>src/map/skills/other/ilookuptoyou.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ILookUpToYou : SkillImpl
{
    public ILookUpToYou() : base(SkillIds.WE_FEMALE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 
    // 	uint8 sp_rate = abs(skill_get_sp_rate(getSkillId(), skill_lv));
    // 
    // 	if (sp_rate && status_get_sp(src) > status_get_max_sp(src) / sp_rate) {
    // 		int32 gain_sp = tstatus->max_sp * sp_rate / 100; // The earned is the same % of the target SP than it costed the caster. [Skotlex]
    // 
    // 		clif_skill_nodamage(src,*target,getSkillId(),status_heal(target, 0, gain_sp, 0));
    // 	}
    }
}
