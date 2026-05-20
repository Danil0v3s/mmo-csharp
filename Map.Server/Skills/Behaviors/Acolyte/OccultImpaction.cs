using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_INVESTIGATE — auto-generated stub from
/// <c>src/map/skills/acolyte/occultimpaction.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class OccultImpaction : WeaponSkillImpl
{
    public OccultImpaction() : base(SkillIds.MO_INVESTIGATE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	status_change_end(src, SC_BLADESTOP);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const status_change* tsc = status_get_sc(target);
    // 
    // 	base_skillratio += -100 + 100 * skill_lv;
    // 	if (tsc && tsc->getSCE(SC_BLADESTOP))
    // 		base_skillratio += base_skillratio / 2;
    // #else
    // 	base_skillratio += 75 * skill_lv;
    // #endif
    return baseRatio;
    }
}
