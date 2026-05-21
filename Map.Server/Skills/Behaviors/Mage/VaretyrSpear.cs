using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_VARETYR_SPEAR — Sorcerer Varetyr Spear. Manual port of
/// <c>rathena-fork/src/map/skills/mage/varetyrspear.cpp</c>.
///
/// <para>Wind splash. Ratio:
/// <c>+(-100 + (2*INT + INT*lv/2) / 3)</c>; the +<c>150*(StrikingLv + LightningLoaderLv)</c>
/// term and SC_BLAST_OPTION job_level*5 bonus are TODO. Splash victims
/// roll <c>5*lv %</c> SC_STUN.</para>
/// </summary>
public sealed class VaretyrSpear : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public VaretyrSpear() : base(SkillIds.SO_VARETYR_SPEAR) => _rng = Random.Shared;

    public VaretyrSpear(Random? rng = null) : base(SkillIds.SO_VARETYR_SPEAR) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + (2 * src.Stats.IntStat + src.Stats.IntStat * skillLevel / 2) / 3);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
    }
}
