using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_THIRD_CONSECRATION — Inquisitor Third Consecration. Manual
/// port of <c>rathena-fork/src/map/skills/acolyte/thirdconsecration.cpp</c>.
///
/// <para>Holy splash. Ratio: <c>-100 + 700*lv + 10*POW</c>. Ends
/// <see cref="StatusType.SecondBrand"/> on hit. Heals caster
/// <c>lv % MaxHP + lv % MaxSP</c> on each splash entry.</para>
/// </summary>
public sealed class ThirdConsecration : RecursiveDamageSplashSkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public ThirdConsecration() : base(SkillIds.IQ_THIRD_CONSECRATION) { }

    public ThirdConsecration(IStatusOpsService? statusOps = null) : base(SkillIds.IQ_THIRD_CONSECRATION)
    {
        _statusOps = statusOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 700 * skillLevel) + 10 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.SecondBrand);
        // Heals caster: skill_lv % MaxHP + skill_lv % MaxSP.
        _statusOps?.PercentHeal(src, (sbyte)skillLevel, (sbyte)skillLevel);
    }
}
