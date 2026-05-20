using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AC_OWL (id 43) — Archer Owl's Eye. rAthena: passive that grants
/// +<c>lv</c> DEX permanently. Cast-time activation only fires once
/// (the SC slot doesn't exist; the bonus is folded into status_calc_pc
/// at recalc time).
///
/// Plugin no-ops because the stat is data-pending on the passive
/// skill-tree port; the canonical entry point lives here so a future
/// pc_skill_bonus pass can plug in cleanly.
/// </summary>
public sealed class OwlsEyeBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AC_OWL;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
        => true; // claim cast; stat applies at status_calc_pc.
}
