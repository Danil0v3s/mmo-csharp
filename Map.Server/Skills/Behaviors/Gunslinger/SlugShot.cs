using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_SLUGSHOT — auto-generated stub from
/// <c>src/map/skills/gunslinger/slugshot.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SlugShot : WeaponSkillImpl
{
    public SlugShot() : base(SkillIds.RL_SLUGSHOT) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_STUN, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	if (target->type == BL_MOB) {
    // 		skillratio += -100 + 1200 * skill_lv;
    // 	} else {
    // 		skillratio += -100 + 2000 * skill_lv;
    // 	}
    // 	skillratio *= 2 + tstatus->size;
    return baseRatio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int8 dist = distance_bl(src, target);
    // 
    // 	if (dist > 3) {
    // 		// Reduce n hitrate for each cell after initial 3 cells. Different each level
    // 		// -10:-9:-8:-7:-6
    // 		dist -= 3;
    // 		hit_rate -= ((11 - skill_lv) * dist);
    // 	}
    return hitRate;
    }
}
