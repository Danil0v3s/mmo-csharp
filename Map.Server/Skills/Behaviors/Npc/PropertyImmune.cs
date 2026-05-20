using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_IMMUNE_PROPERTY — auto-generated stub from
/// <c>src/map/skills/npc/propertyimmune.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PropertyImmune : SkillImpl
{
    public PropertyImmune() : base(SkillIds.NPC_IMMUNE_PROPERTY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 
    // 	switch (skill_lv) {
    // 		case 1: type = SC_IMMUNE_PROPERTY_NOTHING; break;
    // 		case 2: type = SC_IMMUNE_PROPERTY_WATER; break;
    // 		case 3: type = SC_IMMUNE_PROPERTY_GROUND; break;
    // 		case 4: type = SC_IMMUNE_PROPERTY_FIRE; break;
    // 		case 5: type = SC_IMMUNE_PROPERTY_WIND; break;
    // 		case 6: type = SC_IMMUNE_PROPERTY_DARKNESS; break;
    // 		case 7: type = SC_IMMUNE_PROPERTY_SAINT; break;
    // 		case 8: type = SC_IMMUNE_PROPERTY_POISON; break;
    // 		case 9: type = SC_IMMUNE_PROPERTY_TELEKINESIS; break;
    // 		case 10: type = SC_IMMUNE_PROPERTY_UNDEAD; break;
    // 	}
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv,sc_start(src,target,type,100,skill_lv,skill_get_time(getSkillId(),skill_lv)));
    }
}
