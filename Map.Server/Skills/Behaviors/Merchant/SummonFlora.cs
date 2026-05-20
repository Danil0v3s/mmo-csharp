using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CANNIBALIZE — auto-generated stub from
/// <c>src/map/skills/merchant/summonflora.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SummonFlora : SkillImpl
{
    public SummonFlora() : base(SkillIds.AM_CANNIBALIZE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 summons[5] = { MOBID_G_MANDRAGORA, MOBID_G_HYDRA, MOBID_G_FLORA, MOBID_G_PARASITE, MOBID_G_GEOGRAPHER };
    // 	int32 class_ = summons[skill_lv-1];
    // 	enum mob_ai ai = AI_FLORA;
    // 	mob_data *md;
    // 
    // 	// Correct info, don't change any of this! [celest]
    // 	md = mob_once_spawn_sub(src, src->m, x, y, status_get_name(*src), class_, "", SZ_SMALL, ai);
    // 	if (md) {
    // 		md->master_id = src->id;
    // 		md->special_state.ai = ai;
    // 		if( md->deletetimer != INVALID_TIMER )
    // 			delete_timer(md->deletetimer, mob_timer_delete);
    // 		md->deletetimer = add_timer (gettick() + skill_get_time(getSkillId(),skill_lv), mob_timer_delete, md->id, 0);
    // 		mob_spawn (md); //Now it is ready for spawning.
    // 	}
    }
}
