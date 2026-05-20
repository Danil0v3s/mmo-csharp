using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_SOUL_VC_STRIKE — auto-generated stub from
/// <c>src/map/skills/mage/soulvulcanstrike.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SoulVulcanStrike : RecursiveDamageSplashSkillImpl
{
    public SoulVulcanStrike() : base(SkillIds.AG_SOUL_VC_STRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 300 * skill_lv + 3 * sstatus->spl;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
