using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_DEMONIC_FIRE — auto-generated stub from
/// <c>src/map/skills/merchant/demonicfire.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DemonicFire : RecursiveDamageSplashSkillImpl
{
    public DemonicFire() : base(SkillIds.GN_DEMONIC_FIRE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (skill_lv > 20)	// Fire expansion Lv.2
    // 		skillratio += 10 + 20 * (skill_lv - 20) + status_get_int(src) * 10;
    // 	else if (skill_lv > 10) { // Fire expansion Lv.1
    // 		skillratio += 10 + 20 * (skill_lv - 10) + status_get_int(src) + ((sd) ? sd->status.job_level : 50);
    // 		RE_LVL_DMOD(100);
    // 	} else
    // 		skillratio += 10 + 20 * skill_lv;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Ammo should be deleted right away.
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
