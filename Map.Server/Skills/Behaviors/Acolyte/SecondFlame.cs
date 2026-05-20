using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_SECOND_FLAME — auto-generated stub from
/// <c>src/map/skills/acolyte/secondflame.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SecondFlame : RecursiveDamageSplashSkillImpl
{
    public SecondFlame() : base(SkillIds.IQ_SECOND_FLAME) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 200 + 2900 * skill_lv + 9 * sstatus->pow;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_SECOND_BRAND, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }
}
