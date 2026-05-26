using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_DEMONIC_FIRE — Genetic Demonic Fire. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/demonicfire.cpp</c>.
/// Drops a fire ground unit. Three damage tiers:
/// <list type="bullet">
///   <item>Base (<c>lv ≤ 10</c>): <c>+10 + 20*lv</c>.</item>
///   <item>Fire Expansion Lv 1 (<c>11..20</c>): <c>+10 + 20*(lv-10) +
///         INT + job_level</c>, with RE_LVL_DMOD(100).</item>
///   <item>Fire Expansion Lv 2 (<c>lv &gt; 20</c>): <c>+10 + 20*(lv-20)
///         + 10*INT</c>.</item>
/// </list>
/// rAthena exposes Fire Expansion lv ≥ 11 by re-issuing the cast with a
/// boosted <paramref name="skillLevel"/>; we follow that contract.
/// </summary>
public sealed class DemonicFire : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public DemonicFire() : base(SkillIds.GN_DEMONIC_FIRE) { }

    public DemonicFire(ISkillUnitService? units = null) : base(SkillIds.GN_DEMONIC_FIRE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        if (skillLevel > 20)
            return baseRatio + 10 + 20 * (skillLevel - 20) + src.Stats.IntStat * 10;
        if (skillLevel > 10)
        {
            // rAthena: ratio += 10 + 20*(lv-10) + INT + job_level; RE_LVL_DMOD(100).
            // job_level only meaningful for PlayerEntity; default to 50 for NPCs.
            var jobLevel = src is PlayerEntity pc ? pc.JobLevel : 50;
            var ratio = baseRatio + 10 + 20 * (skillLevel - 10) + src.Stats.IntStat + jobLevel;
            // RE_LVL_DMOD(100): ratio = ratio * BaseLevel / 100.
            ratio = ratio * Math.Max(1, src.Level) / 100;
            return ratio;
        }
        return baseRatio + 10 + 20 * skillLevel;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
