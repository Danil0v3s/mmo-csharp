using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_PETRIFYATTACK — auto-generated stub from
/// <c>src/map/skills/npc/petrifyattack.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PetrifyAttack : WeaponSkillImpl
{
    public PetrifyAttack() : base(SkillIds.NPC_PETRIFYATTACK) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start2(src,target,SC_STONEWAIT,(20*skill_lv),skill_lv,src->id,skill_get_time2(getSkillId(),skill_lv),skill_get_time(getSkillId(), skill_lv));
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // hit_rate += hit_rate * 20 / 100;
    return hitRate;
    }
}
