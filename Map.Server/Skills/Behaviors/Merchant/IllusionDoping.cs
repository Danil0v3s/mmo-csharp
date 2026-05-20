using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_ILLUSIONDOPING — auto-generated stub from
/// <c>src/map/skills/merchant/illusiondoping.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class IllusionDoping : RecursiveDamageSplashSkillImpl
{
    public IllusionDoping() : base(SkillIds.GN_ILLUSIONDOPING) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( sc_start(src,target,SC_ILLUSIONDOPING,100 - skill_lv * 10,skill_lv,skill_get_time(getSkillId(),skill_lv)) )
    // 		sc_start(src,target,SC_HALLUCINATION,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    }
}
