using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_DOUBLE — auto-generated stub from
/// <c>src/map/skills/thief/doubleattack.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DoubleAttack : WeaponSkillImpl
{
    public DoubleAttack() : base(SkillIds.TF_DOUBLE) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // For NPC used skill.
    // 	dmg.type = DMG_MULTI_HIT;
    }
}
