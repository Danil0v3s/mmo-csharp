using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KUNAIKUSSETSU — Kunai Refraction. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kunairefraction.cpp</c>.
/// Detonator that triggers nearby SS_KUNAIKAITEN units. Ratio
/// <c>+(-100 + 250 + 420*lv) + 5*pow</c>. SS_KUNAIKAITEN partner
/// bonus + detonate iteration is TODO.
/// </summary>
public sealed class KunaiRefraction : SkillImpl
{
    public KunaiRefraction() : base(SkillIds.SS_KUNAIKUSSETSU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 420 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
