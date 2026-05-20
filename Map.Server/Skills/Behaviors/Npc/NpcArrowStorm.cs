using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_ARROWSTORM — auto-generated stub from
/// <c>src/map/skills/npc/npcarrowstorm.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcArrowStorm : RecursiveDamageSplashSkillImpl
{
    public NpcArrowStorm() : base(SkillIds.NPC_ARROWSTORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (skill_lv > 4)
    // 		base_skillratio += 1900;
    // 	else
    // 		base_skillratio += 900;
    return baseRatio;
    }
}
