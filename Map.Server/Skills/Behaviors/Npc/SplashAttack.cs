using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_SPLASHATTACK — auto-generated stub from
/// <c>src/map/skills/npc/splashattack.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SplashAttack : RecursiveDamageSplashSkillImpl
{
    public SplashAttack() : base(SkillIds.NPC_SPLASHATTACK) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag |= SD_PREAMBLE; // a fake packet will be sent for the first target to be hit
    // 
    // 	SkillImplRecursiveDamageSplash::castendDamageId(src, target, skill_lv, tick, flag);
    }
}
