using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_HERMODE — auto-generated stub from
/// <c>src/map/skills/archer/wandofhermode.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WandOfHermode : SkillImpl
{
    public WandOfHermode() : base(SkillIds.CG_HERMODE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	skill_castend_song(src, getSkillId(), skill_lv, tick);
    // #endif
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifndef RENEWAL
    // 	skill_clear_unitgroup(src);
    // 	if (auto sg = skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0); sg != nullptr)
    // 		sc_start4(src, src, SC_DANCING, 100, getSkillId(), 0, skill_lv, sg->group_id, skill_get_time(getSkillId(), skill_lv));
    // 	flag |= 1;
    // #endif
    }
}
