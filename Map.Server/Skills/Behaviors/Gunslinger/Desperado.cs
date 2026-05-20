using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DESPERADO — auto-generated stub from
/// <c>src/map/skills/gunslinger/desperado.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Desperado : SkillImpl
{
    public Desperado() : base(SkillIds.GS_DESPERADO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 
    // 	base_skillratio += 50 * (skill_lv - 1);
    // 	if (sc && sc->getSCE(SC_FALLEN_ANGEL))
    // 		base_skillratio *= 2;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag |= 1;
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
    }
}
