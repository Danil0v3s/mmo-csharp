using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_PNEUMA — Pneuma. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/pneuma.cpp</c>.
///
/// <para>Ground-target placement skill: drops a 1-cell Pneuma unit
/// that blocks incoming ranged attacks on anyone standing in it.
/// The unit lives for 10 s and the SkillUnit engine handles the
/// per-tick effect (NoEnemy / NoReiteration flags from skill_db
/// prevent stacking against the caster's enemies).</para>
///
/// <para>Implementation is a single call to
/// <see cref="ISkillUnitService.Place"/>. The fork's <c>flag |= 1</c>
/// is an rAthena hint to skip ammo decrement — we don't model
/// ammo consumption for self-cast spells, so the flag is a no-op
/// here.</para>
/// </summary>
public sealed class Pneuma : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Pneuma() : base(SkillIds.AL_PNEUMA) { }

    public Pneuma(ISkillUnitService? units = null) : base(SkillIds.AL_PNEUMA)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
        // The skill_db unit definition (Id: Pneuma, Layout: 1, Interval: -1)
        // drives the placement: a single cell, no periodic tick (the
        // ranged-block check happens on each incoming attack via the
        // unit's OnPlace hook).
        _units?.Place(src, SkillId, skillLevel, x, y);
    }
}
