using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_DECAGI — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_decreaseagi.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryDecreaseAgi : SkillImpl
{
    public MercenaryDecreaseAgi() : base(SkillIds.MER_DECAGI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* sstatus = status_get_status_data(*src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv,
    // 		sc_start(src,target, type, (50 + skill_lv * 3 + (status_get_lv(src) + sstatus->int_)/5), skill_lv, skill_get_time(getSkillId(),skill_lv)));
    }
}
