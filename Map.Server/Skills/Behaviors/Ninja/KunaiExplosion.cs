using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_BAKURETSU — Kunai Explosion. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kunaiexplosion.cpp</c>.
/// Recursive splash; ratio uses NJ_TOBIDOUGU level * (50 + dex/4) * lv
/// * 4 / 10 plus 10 * jobLv plus Kagemusya multiplier. Partner-skill
/// + Kagemusya scaling are TODO.
/// </summary>
public sealed class KunaiExplosion : RecursiveDamageSplashSkillImpl
{
    public KunaiExplosion() : base(SkillIds.KO_BAKURETSU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + -100 + 1 * (50 + src.Stats.Dex / 4) * skillLevel * 4 / 10;
        ratio += 10;
        return ratio;
    }
}
