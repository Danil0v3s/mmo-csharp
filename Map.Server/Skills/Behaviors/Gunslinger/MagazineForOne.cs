using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_MAGAZINE_FOR_ONE — Night Watch Magazine For One. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/magazineforone.cpp</c>.
/// Ratio <c>+(-100 + 250 + 500*lv) + 5*CON</c>; SC_INTENSIVE_AIM_COUNT
/// and revolver weapon bonuses are TODO.
/// </summary>
public sealed class MagazineForOne : WeaponSkillImpl
{
    public MagazineForOne() : base(SkillIds.NW_MAGAZINE_FOR_ONE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 500 * skillLevel) + 5 * src.Stats.Con;
}
