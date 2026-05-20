using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_METALIC_FURY — auto-generated stub from
/// <c>src/map/skills/archer/metallicfury.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MetallicFury : RecursiveDamageSplashSkillImpl
{
    public MetallicFury() : base(SkillIds.TR_METALIC_FURY) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* tsc = status_get_sc(target);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 3850 * skill_lv;
    // 	// !Todo: skill affected by SPL (without SC_SOUNDBLEND) as well?
    // 	if (tsc && tsc->getSCE(SC_SOUNDBLEND)) {
    // 		skillratio += 800 * skill_lv;
    // 		skillratio += 2 * pc_checkskill(sd, TR_STAGE_MANNER) * sstatus->spl;
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
