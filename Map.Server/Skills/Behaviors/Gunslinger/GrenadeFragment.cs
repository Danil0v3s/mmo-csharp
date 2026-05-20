using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_GRENADE_FRAGMENT — auto-generated stub from
/// <c>src/map/skills/gunslinger/grenadefragment.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GrenadeFragment : SkillImpl
{
    public GrenadeFragment() : base(SkillIds.NW_GRENADE_FRAGMENT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change_end(src, skill_get_sc(getSkillId()));
    // 	if (skill_lv < 7)
    // 		sc_start(src, target, (sc_type)(SC_GRENADE_FRAGMENT_1 -1 + skill_lv), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	else if (skill_lv == 7) {
    // 		status_change_end(src, SC_GRENADE_FRAGMENT_1);
    // 		status_change_end(src, SC_GRENADE_FRAGMENT_2);
    // 		status_change_end(src, SC_GRENADE_FRAGMENT_3);
    // 		status_change_end(src, SC_GRENADE_FRAGMENT_4);
    // 		status_change_end(src, SC_GRENADE_FRAGMENT_5);
    // 		status_change_end(src, SC_GRENADE_FRAGMENT_6);
    // 	}
    // 	clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    }
}
