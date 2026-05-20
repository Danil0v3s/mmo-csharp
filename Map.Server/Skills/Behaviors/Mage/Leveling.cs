using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_LEVELUP — auto-generated stub from
/// <c>src/map/skills/mage/leveling.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Leveling : SkillImpl
{
    public Leveling() : base(SkillIds.SA_LEVELUP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	if (sd && pc_nextbaseexp(sd))
    // 		pc_gainexp(sd, nullptr, pc_nextbaseexp(sd) * 10 / 100, 0, 0);
    }
}
