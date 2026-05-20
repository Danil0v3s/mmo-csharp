using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_METALICSOUND — auto-generated stub from
/// <c>src/map/skills/archer/metallicsound.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MetallicSound : SkillImpl
{
    public MetallicSound() : base(SkillIds.WM_METALICSOUND) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change_end(target, SC_SOUNDBLEND);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 120 * skill_lv + 60 * ((sd) ? pc_checkskill(sd, WM_LESSON) : 1);
    // 	if (tsc && tsc->getSCE(SC_SLEEP))
    // 		skillratio += 100; // !TODO: Confirm target sleeping bonus
    // 	RE_LVL_DMOD(100);
    // 	if (tsc && tsc->getSCE(SC_SOUNDBLEND))
    // 		skillratio += skillratio * 50 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }
}
