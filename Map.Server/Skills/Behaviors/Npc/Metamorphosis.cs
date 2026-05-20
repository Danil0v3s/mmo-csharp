using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_METAMORPHOSIS — auto-generated stub from
/// <c>src/map/skills/npc/metamorphosis.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Metamorphosis : SkillImpl
{
    public Metamorphosis() : base(SkillIds.NPC_METAMORPHOSIS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	if(md && md->skill_idx >= 0) {
    // 		int32 class_ = mob_random_class (md->db->skill[md->skill_idx]->val,0);
    // 		if (skill_lv > 1) //Multiply the rest of mobs. [Skotlex]
    // 			mob_summonslave(md,md->db->skill[md->skill_idx]->val,skill_lv-1,getSkillId());
    // 		if (class_) mob_class_change(md, class_);
    // 	}
    }
}
