using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGBITE — Ranger Warg Bite. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargbite.cpp</c>.
/// Ratio <c>+(300 + 200*lv)</c> (+100 at lv 5). On-hit SC_BITE at
/// <c>max(50, 50 + 10*lv - target_AGI/4) %</c>.
/// </summary>
public sealed class WargBite : WeaponSkillImpl
{
    private readonly Random _rng;

    public WargBite() : base(SkillIds.RA_WUGBITE) => _rng = Random.Shared;

    public WargBite(Random? rng = null) : base(SkillIds.RA_WUGBITE) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 300 + 200 * skillLevel;
        if (skillLevel == 5) ratio += 100;
        return ratio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = Math.Max(50, 50 + 10 * skillLevel - target.Stats.Agi / 4);
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Bite, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
