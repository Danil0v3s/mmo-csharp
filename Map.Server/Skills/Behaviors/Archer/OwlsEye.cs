using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_OWL — Archer Owl's Eye. Mirrors
/// <c>rathena-fork/src/map/skills/archer/owlseye.cpp</c>.
///
/// Passive +lv DEX. Cast claims the slot — the stat bonus is
/// folded into status_calc_pc at recalc time (passive port pending).
/// </summary>
public sealed class OwlsEye : SkillImpl
{
    public OwlsEye() : base(SkillIds.AC_OWL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Passive — no SC; pc_skill bonus pass owns the stat fold.
    }
}
