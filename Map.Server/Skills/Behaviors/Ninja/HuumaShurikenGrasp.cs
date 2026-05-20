using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_FUUMASHOUAKU — auto-generated stub from
/// <c>src/map/skills/ninja/huumashurikengrasp.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HuumaShurikenGrasp : SkillImpl
{
    public HuumaShurikenGrasp() : base(SkillIds.SS_FUUMASHOUAKU) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 850 + 350 * skill_lv;
    // 	skillratio += pc_checkskill( sd, SS_FUUMAKOUCHIKU ) * 5 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Ammo should be deleted right away.
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
