using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_VIOLENTGALE — auto-generated stub from
/// <c>src/map/skills/mage/whirlwind.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Whirlwind : SkillImpl
{
    public Whirlwind() : base(SkillIds.SA_VIOLENTGALE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Does not consumes if the skill is already active. [Skotlex]
    // 	std::shared_ptr<s_skill_unit_group> sg2;
    // 	if ((sg2= skill_locate_element_field(src)) != nullptr && ( sg2->skill_id == SA_VOLCANO || sg2->skill_id == SA_DELUGE || sg2->skill_id == SA_VIOLENTGALE ))
    // 	{
    // 		if (sg2->limit - DIFF_TICK(gettick(), sg2->tick) > 0)
    // 		{
    // 			skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    // 			flag |= SKILL_NOCONSUME_REQ; // not to consume items
    // 			return;
    // 		}
    // 		else
    // 			sg2->limit = 0; //Disable it.
    // 	}
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
