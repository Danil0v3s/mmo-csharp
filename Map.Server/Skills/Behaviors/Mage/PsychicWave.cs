using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_PSYCHIC_WAVE — Sorcerer Psychic Wave. Manual port of
/// <c>rathena-fork/src/map/skills/mage/psychicwave.cpp</c>.
///
/// <para>Ground unit Neutral magic. Ratio: <c>+(-100 + 70*lv) + 3*INT</c>,
/// with +20 when any Spirit Option (HEATER/COOLER/BLAST/CURSED_SOIL)
/// is active on the caster. Hit count doubles when the caster
/// wields a staff (W_STAFF / W_2HSTAFF) or book (W_BOOK).</para>
/// </summary>
public sealed class PsychicWave : SkillImpl
{
    /// <summary>rAthena <c>W_STAFF</c> = 10 (pc.hpp).</summary>
    private const int W_STAFF = 10;
    /// <summary>rAthena <c>W_BOOK</c> = 15 (pc.hpp).</summary>
    private const int W_BOOK = 15;
    /// <summary>rAthena <c>W_2HSTAFF</c> = 23 (pc.hpp).</summary>
    private const int W_2HSTAFF = 23;

    private readonly ISkillUnitService? _units;

    public PsychicWave() : base(SkillIds.SO_PSYCHIC_WAVE) { }

    public PsychicWave(ISkillUnitService? units = null) : base(SkillIds.SO_PSYCHIC_WAVE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 70 * skillLevel) + 3 * src.Stats.IntStat;
        if (ctx.Sc != null && (
            ctx.Sc.Get(src, StatusType.HeaterOption) != null ||
            ctx.Sc.Get(src, StatusType.CoolerOption) != null ||
            ctx.Sc.Get(src, StatusType.BlastOption) != null ||
            ctx.Sc.Get(src, StatusType.CursedSoilOption) != null))
        {
            ratio += 20;
        }
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: dmg.div_ = 2 when caster wields W_STAFF / W_2HSTAFF / W_BOOK.
        if (src is PlayerEntity pc &&
            (pc.WeaponType == W_STAFF || pc.WeaponType == W_2HSTAFF || pc.WeaponType == W_BOOK))
        {
            dmg.Hits = 2;
        }
    }
}
