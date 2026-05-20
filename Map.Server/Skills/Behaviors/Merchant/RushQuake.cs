using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_RUSH_QUAKE — auto-generated stub from
/// <c>src/map/skills/merchant/rushquake.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RushQuake : RecursiveDamageSplashSkillImpl
{
    public RushQuake() : base(SkillIds.MT_RUSH_QUAKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	skillratio += -100 + 3600 * skill_lv + 10 * sstatus->pow;
    // 	if (tstatus->race == RC_FORMLESS || tstatus->race == RC_INSECT)
    // 		skillratio += 150 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start( src, target, SC_RUSH_QUAKE1, 100, skill_lv, skill_get_time( getSkillId(), skill_lv ) );
    }
}
