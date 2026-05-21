using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_POISON_BUSTER — Sorcerer Poison Buster. Manual port of
/// <c>rathena-fork/src/map/skills/mage/poisonbuster.cpp</c>.
///
/// <para>Splash skill. Ratio: <c>+(-100 + 1000 + 300*lv) + INT</c>,
/// with +<c>200*lv</c> when SC_CLOUD_POISON is on the target and
/// +<c>job_level*5</c> with SC_CURSED_SOIL_OPTION on the caster — both
/// SC reads are TODO (no SC readback in this hook yet).</para>
/// </summary>
public sealed class PoisonBuster : RecursiveDamageSplashSkillImpl
{
    public PoisonBuster() : base(SkillIds.SO_POISON_BUSTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 1000 + 300 * skillLevel) + src.Stats.IntStat;
    }
}
