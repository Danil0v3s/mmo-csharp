using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_AGE_OF_ICE — auto-generated stub from
/// <c>src/map/skills/elemental/ageofice.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AgeOfIce : RecursiveDamageSplashSkillImpl
{
    public AgeOfIce() : base(SkillIds.EM_EL_AGE_OF_ICE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const s_elemental_data* ed = BL_CAST(BL_ELEM, src);
    // 
    // 	base_skillratio += -100 + 3700;
    // 	if (ed)
    // 		base_skillratio += base_skillratio * status_get_lv(ed->master) / 100;
    return baseRatio;
    }
}
