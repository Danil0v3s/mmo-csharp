using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_RAYOFGENESIS — Royal Guard Ray of Genesis. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/rayofgenesis.cpp</c>.
/// Ratio <c>+(-100 + 350*lv) + 3*INT</c>. 50% blind vs Undead element
/// or Demon race.
/// </summary>
public sealed class RayOfGenesis : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public RayOfGenesis() : base(SkillIds.LG_RAYOFGENESIS) => _rng = Random.Shared;

    public RayOfGenesis(Random? rng = null) : base(SkillIds.LG_RAYOFGENESIS)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 * skillLevel) + 3 * src.Stats.IntStat;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var isUndead = target.Stats.DefenseElement == BattleElement.Undead;
        if (!isUndead && target.Stats.Race != BattleRace.Demon) return;
        if (_rng.Next(100) < 50)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
