using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_CURE — auto-generated stub from
/// <c>src/map/skills/acolyte/cure.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Cure : SkillImpl
{
    public Cure() : base(SkillIds.AL_CURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (status_isimmune(bl))
    // 	{
    // 		clif_skill_nodamage(src, *bl, getSkillId(), skill_lv, false);
    // 		return;
    // 	}
    // 	status_change_end(bl, SC_SILENCE);
    // 	status_change_end(bl, SC_BLIND);
    // 	status_change_end(bl, SC_CONFUSION);
    // 	status_change_end(bl, SC_BITESCAR);
    // 	clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
    }
}
