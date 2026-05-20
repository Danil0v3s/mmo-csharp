using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.LordKnight;

/// <summary>
/// LK_CONCENTRATION — Lord Knight Concentration. Mirrors
/// <c>rathena-fork/src/map/skills/lordknight/lkconcentration.cpp</c>.
///
/// Apply <see cref="StatusType.Concentration"/> on caster:
/// +2*lv % ATK, +1*lv % Hit, but takes 5 % more damage taken
/// (consumer for the +damage-taken modifier ports later).
/// Duration <c>30 + 30*lv</c>s.
///
/// Named "LkConcentration" to disambiguate from the Mage-side
/// SC_CONCENTRATE (Awakening Potion's stat-mod SC).
/// </summary>
public sealed class LkConcentration : SkillImpl
{
    public LkConcentration() : base(SkillIds.LK_CONCENTRATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Concentration, val1: skillLevel, 0, 0, 0,
            durationMs: (30 + 30 * skillLevel) * 1000, src);
    }
}
