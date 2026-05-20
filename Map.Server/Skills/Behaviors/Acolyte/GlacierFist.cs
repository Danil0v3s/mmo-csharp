using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_TIGERFIST — auto-generated stub from
/// <c>src/map/skills/acolyte/glacierfist.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GlacierFist : WeaponSkillImpl
{
    public GlacierFist() : base(SkillIds.CH_TIGERFIST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	skillratio += 400 + 150 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // #else
    // 	skillratio += -60 + 100 * skill_lv;
    // #endif
    // 	if (const status_change* sc = status_get_sc(src); sc != nullptr && sc->getSCE(SC_GT_ENERGYGAIN))
    // 		skillratio += skillratio * 50 / 100;
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // t_tick basetime = skill_get_time(getSkillId(), skill_lv);
    // 	t_tick mintime = 15 * (status_get_lv(src) + 100);
    // 
    // 	if (status_bl_has_mode(target, MD_STATUSIMMUNE))
    // 		basetime /= 5;
    // 	basetime = std::max((basetime * status_get_agi(target)) / -200 + basetime, mintime);
    // 	sc_start(src, target, SC_ANKLE, (1 + skill_lv) * 10, 0, basetime);
    }
}
