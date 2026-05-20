using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_PIERCINGSHOT — auto-generated stub from
/// <c>src/map/skills/gunslinger/piercingshot.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PiercingShot : WeaponSkillImpl
{
    public PiercingShot() : base(SkillIds.GS_PIERCINGSHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd && sd->weapontype1 == W_RIFLE)
    // 		base_skillratio += 150 + 30 * skill_lv;
    // 	else
    // 		base_skillratio += 100 + 20 * skill_lv;
    // #else
    // 	base_skillratio += 20 * skill_lv;
    // #endif
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start2(src, target, SC_BLEEDING, (skill_lv * 3), skill_lv, src->id, skill_get_time2(getSkillId(), skill_lv));
    }
}
