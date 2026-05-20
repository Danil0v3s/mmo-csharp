using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_BULLSEYE — auto-generated stub from
/// <c>src/map/skills/gunslinger/bullseye.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Bullseye : WeaponSkillImpl
{
    public Bullseye() : base(SkillIds.GS_BULLSEYE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data *tstatus = status_get_status_data(*target);
    // 
    // 	// Only works well against brute/demihumans non bosses.
    // 	if ((tstatus->race == RC_BRUTE || tstatus->race == RC_DEMIHUMAN || tstatus->race == RC_PLAYER_HUMAN || tstatus->race == RC_PLAYER_DORAM) && !status_has_mode(
    // 		    tstatus, MD_STATUSIMMUNE))
    // 		base_skillratio += 400;
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data *tstatus = status_get_status_data(*target);
    // 
    // 	// 0.1% coma rate.
    // 	if (tstatus->race == RC_BRUTE || tstatus->race == RC_DEMIHUMAN || tstatus->race == RC_PLAYER_HUMAN || tstatus->race == RC_PLAYER_DORAM)
    // 		status_change_start(src, target, SC_COMA, 10, skill_lv, 0, src->id, 0, 0, SCSTART_NONE);
    }
}
