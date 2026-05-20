using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_BLEEDING2 — auto-generated stub from
/// <c>src/map/skills/npc/bleeding2.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Bleeding2 : WeaponSkillImpl
{
    public Bleeding2() : base(SkillIds.NPC_BLEEDING2) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_BLEEDING,(50+10*skill_lv),skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // hit_rate += hit_rate * 20 / 100;
    return hitRate;
    }
}
