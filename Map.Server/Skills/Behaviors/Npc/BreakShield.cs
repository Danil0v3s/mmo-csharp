using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_SHIELDBRAKE — auto-generated stub from
/// <c>src/map/skills/npc/breakshield.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BreakShield : WeaponSkillImpl
{
    public BreakShield() : base(SkillIds.NPC_SHIELDBRAKE) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_break_equip(src,target, EQP_SHIELD, 150*skill_lv, BCT_ENEMY);
    }
}
