using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_SCAPEGOAT — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_scapegoat.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryScapegoat : SkillImpl
{
    public MercenaryScapegoat() : base(SkillIds.MER_SCAPEGOAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // s_mercenary_data* mer = BL_CAST(BL_MER, src);
    // 
    // 	if( mer && mer->master )
    // 	{
    // 		status_heal(mer->master, mer->battle_status.hp, 0, 2);
    // 		status_damage(src, src, mer->battle_status.max_hp, 0, 0, 1, getSkillId());
    // 	}
    }
}
