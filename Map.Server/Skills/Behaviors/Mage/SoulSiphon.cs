using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_SOULBURN — auto-generated stub from
/// <c>src/map/skills/mage/soulsiphon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SoulSiphon : SkillImpl
{
    public SoulSiphon() : base(SkillIds.PF_SOULBURN) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (rnd() % 100 < (skill_lv < 5 ? 30 + skill_lv * 10 : 70)) {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		if (skill_lv == 5) {
    // 			skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    // 		}
    // 		status_percent_damage(src, target, 0, 100, false);
    // 	} else {
    // 		clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    // 		if (skill_lv == 5) {
    // 			skill_attack(BF_MAGIC, src, src, src, getSkillId(), skill_lv, tick, flag);
    // 		}
    // 		status_percent_damage(src, src, 0, 100, false);
    // 	}
    }
}
