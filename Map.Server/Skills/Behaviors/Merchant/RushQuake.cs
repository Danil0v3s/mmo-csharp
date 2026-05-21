using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_RUSH_QUAKE — Meister Rush Quake. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/rushquake.cpp</c>.
/// Ratio <c>+(-100 + 3600*lv) + 10*POW</c>, additional <c>+150*lv</c>
/// against Formless / Insect races. On hit applies SC_RUSH_QUAKE1.
/// </summary>
public sealed class RushQuake : RecursiveDamageSplashSkillImpl
{
    public RushQuake() : base(SkillIds.MT_RUSH_QUAKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 3600 * skillLevel) + 10 * src.Stats.Pow;
        if (target.Stats.Race == BattleRace.Formless || target.Stats.Race == BattleRace.Insect)
            ratio += 150 * skillLevel;
        return ratio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.RushQuake1, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
}
