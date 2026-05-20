using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_PANGVOICE — auto-generated stub from
/// <c>src/map/skills/archer/pangvoice.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PangVoice : SkillImpl
{
    public PangVoice() : base(SkillIds.BA_PANGVOICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	// In Renewal it causes Confusion and Bleeding to 100% base chance
    // 	sc_start(src, target, SC_CONFUSION, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	sc_start(src, target, SC_BLEEDING, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // #else
    // 	// In Pre-renewal it causes Confusion to 70% base chance
    // 	sc_start(src, target, SC_CONFUSION, 70, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // #endif
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    }
}
