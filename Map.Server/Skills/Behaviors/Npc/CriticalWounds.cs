using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_CRITICALWOUND — auto-generated stub from
/// <c>src/map/skills/npc/criticalwounds.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CriticalWounds : WeaponSkillImpl
{
    public CriticalWounds() : base(SkillIds.NPC_CRITICALWOUND) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_CRITICALWOUND,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }
}
