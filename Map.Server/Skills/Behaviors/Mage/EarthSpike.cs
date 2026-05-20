using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_EARTHSPIKE — auto-generated stub from
/// <c>src/map/skills/mage/earthspike.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EarthSpike : SkillImpl
{
    public EarthSpike() : base(SkillIds.WZ_EARTHSPIKE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC,src,src,target,getSkillId(),skill_lv,tick,flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const status_change* sc = status_get_sc(src);
    // 
    // 	base_skillratio += 100;
    // 	if (sc && sc->getSCE(SC_EARTH_CARE_OPTION))
    // 		base_skillratio += base_skillratio * 800 / 100;
    // #endif
    return baseRatio;
    }
}
