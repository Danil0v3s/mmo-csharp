using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ELEMENTAL_VEIL — auto-generated stub from
/// <c>src/map/skills/mage/elementalveil.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ElementalVeil : SkillImpl
{
    public ElementalVeil() : base(SkillIds.EM_ELEMENTAL_VEIL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 	if (sd == nullptr)
    // 		return;
    // 
    // 	if (sd->ed && sd->ed->elemental.class_ >= ELEMENTALID_DILUVIO && sd->ed->elemental.class_ <= ELEMENTALID_SERPENS)
    // 		sc_start(src, sd->ed, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	else
    // 		clif_skill_fail( *sd, getSkillId() );
    }
}
