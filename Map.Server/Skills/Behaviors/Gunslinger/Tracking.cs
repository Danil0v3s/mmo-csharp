using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_TRACKING — Gunslinger Tracking. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/tracking.cpp</c>.
/// Ratio <c>+100*(lv+1)</c>.
/// </summary>
public sealed class Tracking : WeaponSkillImpl
{
    public Tracking() : base(SkillIds.GS_TRACKING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel + 1);
}
