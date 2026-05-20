using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_FATAL_SHADOW_CROW — auto-generated stub from
/// <c>src/map/skills/thief/fatalshadowcrow.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FatalShadowCrow : RecursiveDamageSplashSkillImpl
{
    public FatalShadowCrow() : base(SkillIds.SHC_FATAL_SHADOW_CROW) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	sc_start( src, target, SC_DARKCROW, 100, max( 1, pc_checkskill( sd, GC_DARKCROW ) ), skill_get_time( getSkillId(), skill_lv ) );
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	skillratio += -100 + 1300 * skill_lv + 10 * sstatus->pow;
    // 	if (tstatus->race == RC_DEMIHUMAN || tstatus->race == RC_DRAGON)
    // 		skillratio += 150 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
