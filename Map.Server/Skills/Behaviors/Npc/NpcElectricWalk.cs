using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_ELECTRICWALK — auto-generated stub from
/// <c>src/map/skills/npc/npcelectricwalk.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcElectricWalk : SkillImpl
{
    public NpcElectricWalk() : base(SkillIds.NPC_ELECTRICWALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += -100 + 100 * skill_lv;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if( sc && sc->getSCE(type) )
    // 		status_change_end(src,type);
    // 	sc_start2(src, src, type, 100, getSkillId(), skill_lv, skill_get_time(getSkillId(), skill_lv));
    }
}
