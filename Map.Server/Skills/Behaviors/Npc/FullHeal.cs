using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_ALLHEAL — auto-generated stub from
/// <c>src/map/skills/npc/fullheal.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FullHeal : SkillImpl
{
    public FullHeal() : base(SkillIds.NPC_ALLHEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( status_isimmune(target) )
    // 		return;
    // 
    // 	mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	int32 heal = status_percent_heal(target, 100, 0);
    // 
    // 	clif_skill_nodamage(nullptr, *target, AL_HEAL, heal);
    // 	if( dstmd )
    // 	{ // Reset Damage Logs
    // 		dstmd->dmglog.clear();
    // 	}
    }
}
