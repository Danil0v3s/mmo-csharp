using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LK_JOINTBEAT — auto-generated stub from
/// <c>src/map/skills/swordman/vitalstrike.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class VitalStrike : SkillImpl
{
    public VitalStrike() : base(SkillIds.LK_JOINTBEAT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 
    // 	base_skillratio += 10 * skill_lv - 50;
    // 
    // 	// The 2x damage is only for the BREAK_NECK ailment.
    // 	if (wd->miscflag & BREAK_NECK || (tsc && tsc->getSCE(SC_JOINTBEAT) && tsc->getSCE(SC_JOINTBEAT)->val2 & BREAK_NECK))
    // 		base_skillratio *= 2;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 	status_change *tsc = status_get_sc(target);
    // 
    // 	flag = 1 << rnd() % 6;
    // 	if (flag != BREAK_NECK && tsc && tsc->getSCE(SC_JOINTBEAT) && tsc->getSCE(SC_JOINTBEAT)->val2 & BREAK_NECK)
    // 		flag = BREAK_NECK; // Target should always receive double damage if neck is already broken
    // 	if (skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, flag))
    // 		status_change_start(src, target, SC_JOINTBEAT, (50 * (skill_lv + 1) - (270 * tstatus->str) / 100) * 10, skill_lv, flag & BREAK_FLAGS, src->id, 0, skill_get_time2(getSkillId(), skill_lv), SCSTART_NONE);
    }
}
