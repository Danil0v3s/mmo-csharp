using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_OWL — Archer Owl's Eye. Mirrors
/// <c>rathena-fork/src/map/skills/archer/owlseye.cpp</c>.
///
/// <para>Pure passive (+lv DEX). rAthena's <c>owlseye.cpp</c> is an
/// empty class shell — the DEX bump is folded into
/// <c>status_calc_pc</c> at recalc time. No-op on cast is the
/// canonical behavior; the stat fold lives in the C# port's
/// <c>StatusCalcPc</c> equipment / passive pipeline rather than in a
/// per-skill hook here.</para>
/// </summary>
public sealed class OwlsEye : SkillImpl
{
    public OwlsEye() : base(SkillIds.AC_OWL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Passive — no SC; the +lv DEX bump is applied by status_calc_pc
        // when the caster's stats are recomputed. No on-cast side effect
        // (matches rAthena's empty owlseye.cpp body).
    }
}
