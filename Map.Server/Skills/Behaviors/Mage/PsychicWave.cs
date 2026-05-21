using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_PSYCHIC_WAVE — Sorcerer Psychic Wave. Manual port of
/// <c>rathena-fork/src/map/skills/mage/psychicwave.cpp</c>.
///
/// <para>Ground unit Neutral magic. Ratio: <c>+(-100 + 70*lv) + 3*INT</c>,
/// with +20 when any Spirit Option (HEATER/COOLER/BLAST/CURSED_SOIL)
/// is active on the caster (SC readback TODO). Hit count doubles when
/// the caster wields a staff/book (weapon-type lookup TODO).</para>
/// </summary>
public sealed class PsychicWave : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PsychicWave() : base(SkillIds.SO_PSYCHIC_WAVE) { }

    public PsychicWave(ISkillUnitService? units = null) : base(SkillIds.SO_PSYCHIC_WAVE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 70 * skillLevel) + 3 * src.Stats.IntStat;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: dmg.div_ = 2 when caster's weapon is W_STAFF / W_2HSTAFF / W_BOOK. Weapon-type read TODO.
    }
}
