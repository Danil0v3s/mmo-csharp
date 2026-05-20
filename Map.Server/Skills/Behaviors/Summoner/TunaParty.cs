using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_TUNAPARTY — auto-generated stub from
/// <c>src/map/skills/summoner/tunaparty.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TunaParty : SkillImpl
{
    public TunaParty() : base(SkillIds.SU_TUNAPARTY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(target,*target,getSkillId(),skill_lv,
    // 		sc_start(src,target,skill_get_sc(getSkillId()),100,skill_lv,skill_get_time(getSkillId(),skill_lv)));
    }
}
