using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_GENTLETOUCH_QUIET — auto-generated stub from
/// <c>src/map/skills/acolyte/gentletouchquiet.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GentleTouchQuiet : WeaponSkillImpl
{
    public GentleTouchQuiet() : base(SkillIds.SR_GENTLETOUCH_QUIET) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // [(Skill Level x 5) + (Caster?s DEX + Caster?s Base Level) / 10]
    // 	sc_start(src,target, SC_SILENCE, 5 * skill_lv + (status_get_dex(src) + status_get_lv(src)) / 10, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 100 * skill_lv + sstatus->dex;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
