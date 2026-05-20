using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_ALL_STAT_DOWN — auto-generated stub from
/// <c>src/map/skills/npc/decreaseallstats.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DecreaseAllStats : SkillImpl
{
    public DecreaseAllStats() : base(SkillIds.NPC_ALL_STAT_DOWN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change_start(src, target, skill_get_sc(getSkillId()), 10000, skill_lv, 0, 0, 0, skill_get_time(getSkillId(), skill_lv), SCSTART_NOAVOID|SCSTART_NOTICKDEF|SCSTART_NORATEDEF);
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    }
}
