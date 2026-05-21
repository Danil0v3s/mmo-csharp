using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_PETITIO — Cardinal Petitio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/petitio.cpp</c>.
///
/// <para>Mace-class melee splash. Ratio scales with skill level
/// plus CD_MACE_BOOK_M passive bonus and POW trait stat:</para>
/// <code>
///   ratio = (-100) + (1050 + 50 * MaceBookLv) * lv + 5 * POW
/// </code>
/// <para>CD_MACE_BOOK_M isn't on our SkillIds catalog yet (Cardinal
/// passives are a separate wave); we omit its contribution and
/// land the base + POW term faithfully.</para>
/// </summary>
public sealed class Petitio : RecursiveDamageSplashSkillImpl
{
    public Petitio() : base(SkillIds.CD_PETITIO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + (1050 + 50*MaceBook) * lv + 5*POW;  RE_LVL_DMOD(100);
        // Omitting MaceBook bonus (passive not wired).
        return baseRatio + (-100 + 1050 * skillLevel) + 5 * src.Stats.Pow;
    }
}
