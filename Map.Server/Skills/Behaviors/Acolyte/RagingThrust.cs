using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_COMBOFINISH — Monk Raging Thrust. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ragingthrust.cpp</c>.
///
/// <para>Standard Monk combo finisher. Under Soul-Linked Monk
/// (SC_SPIRIT val2 = SL_MONK) becomes a splash attack; otherwise
/// single-target weapon hit.</para>
///
/// <para>Renewal ratio: <c>+450 + 50*lv + STR</c>. SC_GT_ENERGYGAIN
/// bonus (+50 %) is TODO (SC reader not in this hook).</para>
/// </summary>
public sealed class RagingThrust : WeaponSkillImpl
{
    public RagingThrust() : base(SkillIds.MO_COMBOFINISH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 450 + 50 * skillLevel + src.Stats.Str;
    }
    // CastendDamageId inherits WeaponSkillImpl default — single-target
    // hit. Soul-Linked splash branch deferred until SC reading lands.
}
