using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_SILVERSNIPER — auto-generated stub from
/// <c>src/map/skills/merchant/fawsilversniper.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FawSilverSniper : SkillImpl
{
    public FawSilverSniper() : base(SkillIds.NC_SILVERSNIPER) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = mob_once_spawn_sub(src, src->m, x, y, status_get_name(*src), MOBID_SILVERSNIPER, "", SZ_SMALL, AI_NONE);
    // 	if (md) {
    // 		md->master_id = src->id;
    // 		md->special_state.ai = AI_FAW;
    // 		if (md->deletetimer != INVALID_TIMER) {
    // 			delete_timer(md->deletetimer, mob_timer_delete);
    // 		}
    // 		md->deletetimer = add_timer(gettick() + skill_get_time(getSkillId(), skill_lv), mob_timer_delete, md->id, 0);
    // 		mob_spawn(md);
    // 	}
    }
}
