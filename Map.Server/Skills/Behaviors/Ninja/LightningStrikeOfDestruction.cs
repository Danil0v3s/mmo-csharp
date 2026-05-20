using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_RAIGEKISAI — auto-generated stub from
/// <c>src/map/skills/ninja/lightningstrikeofdestruction.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class LightningStrikeOfDestruction : SkillImpl
{
    public LightningStrikeOfDestruction() : base(SkillIds.NJ_RAIGEKISAI) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // #ifdef RENEWAL
    // 	base_skillratio += 100 * skill_lv;
    // #else
    // 	base_skillratio += 60 + 40 * skill_lv;
    // #endif
    // 	if(sd && sd->spiritcharm_type == CHARM_TYPE_WIND && sd->spiritcharm > 0)
    // 		base_skillratio += 20 * sd->spiritcharm;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag |= 1;
    // 
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
