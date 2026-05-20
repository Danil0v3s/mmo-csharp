using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_PALMSTRIKE — auto-generated stub from
/// <c>src/map/skills/acolyte/ragingpalmstrike.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RagingPalmStrike : SkillImpl
{
    public RagingPalmStrike() : base(SkillIds.CH_PALMSTRIKE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //	Palm Strike takes effect 1sec after casting. [Skotlex]
    // 	// clif_skill_nodamage(src,*target,getSkillId(),skill_lv,false); //Can't make this one display the correct attack animation delay :/
    // 	clif_damage(*src, *target, tick, status_get_amotion(src), 0, -1, 1, DMG_ENDURE, 0, false); //Display an absorbed damage attack.
    // 	skill_addtimerskill(src, tick + (1000 + status_get_amotion(src)), target->id, 0, 0, getSkillId(), skill_lv, BF_WEAPON, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += 100 + 100 * skill_lv + sstatus->str; // !TODO: How does STR play a role?
    // 	RE_LVL_DMOD(100);
    // #else
    // 	skillratio += 100 + 100 * skill_lv;
    // #endif
    return baseRatio;
    }
}
