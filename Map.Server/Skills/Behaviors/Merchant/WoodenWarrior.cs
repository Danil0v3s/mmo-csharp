using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_WOODENWARRIOR — auto-generated stub from
/// <c>src/map/skills/merchant/woodenwarrior.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WoodenWarrior : SkillImpl
{
    public WoodenWarrior() : base(SkillIds.BO_WOODENWARRIOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 
    // 	mob_data *md = mob_once_spawn_sub(src, src->m, src->x, src->y, "--ja--", MOBID_BIONIC_WOODENWARRIOR, "", SZ_SMALL, AI_BIONIC);
    // 
    // 	if (md) {
    // 		md->master_id = src->id;
    // 		md->special_state.ai = AI_BIONIC;
    // 
    // 		if (md->deletetimer != INVALID_TIMER)
    // 			delete_timer(md->deletetimer, mob_timer_delete);
    // 		md->deletetimer = add_timer(gettick() + skill_get_time(getSkillId(), skill_lv), mob_timer_delete, md->id, 0);
    // 		mob_spawn(md);
    // 	}
    }
}
