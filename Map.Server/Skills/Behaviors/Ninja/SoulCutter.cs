using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_SETSUDAN — auto-generated stub from
/// <c>src/map/skills/ninja/soulcutter.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SoulCutter : WeaponSkillImpl
{
    public SoulCutter() : base(SkillIds.KO_SETSUDAN) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Remove soul link when hit.
    // 	status_change_end(target, SC_SPIRIT);
    // 	status_change_end(target, SC_SOULGOLEM);
    // 	status_change_end(target, SC_SOULSHADOW);
    // 	status_change_end(target, SC_SOULFALCON);
    // 	status_change_end(target, SC_SOULFAIRY);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 
    // 	skillratio += 100 * (skill_lv - 1);
    // 	RE_LVL_DMOD(100);
    // 	if (tsc) {
    // 		const status_change_entry *sce;
    // 
    // 		if ((sce = tsc->getSCE(SC_SPIRIT)) || (sce = tsc->getSCE(SC_SOULGOLEM)) || (sce = tsc->getSCE(SC_SOULSHADOW)) || (sce = tsc->getSCE(SC_SOULFALCON)) || (sce = tsc->getSCE(SC_SOULFAIRY))) // Bonus damage added when target is soul linked.
    // 			skillratio += 200 * sce->val1;
    // 	}
    return baseRatio;
    }
}
