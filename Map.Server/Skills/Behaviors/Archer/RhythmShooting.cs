using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_RHYTHMSHOOTING — Trouvere Rhythm Shooting. Manual port of
/// <c>rathena-fork/src/map/skills/archer/rhythmshooting.cpp</c>.
/// Ratio: <c>+(-100 + 550 + 950*lv) + 5*CON</c> (TR_STAGE_MANNER
/// passive scale assumed always on); SC_SOUNDBLEND / SC_MYSTIC_SYMPHONY
/// bonuses TODO.
/// </summary>
public sealed class RhythmShooting : WeaponSkillImpl
{
    public RhythmShooting() : base(SkillIds.TR_RHYTHMSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 550 + 950 * skillLevel) + 5 * src.Stats.Con;
    }
}
