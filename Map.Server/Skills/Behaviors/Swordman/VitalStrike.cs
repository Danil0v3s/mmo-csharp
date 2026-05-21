using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LK_JOINTBEAT — Lord Knight Vital Strike / Joint Beat. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/vitalstrike.cpp</c>.
/// Ratio <c>+(10*lv - 50)</c>. Rolls a random BREAK_* flag and applies
/// SC_JOINTBEAT keyed to that flag. BREAK_NECK doubles damage.
/// Break-flag definitions live in rAthena status.hpp; we encode the
/// 6 raw bit positions.
/// </summary>
public sealed class VitalStrike : SkillImpl
{
    private readonly Random _rng;

    public VitalStrike() : base(SkillIds.LK_JOINTBEAT) => _rng = Random.Shared;

    public VitalStrike(Random? rng = null) : base(SkillIds.LK_JOINTBEAT)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel - 50;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var flag = 1 << _rng.Next(6); // BREAK_ANKLE / WAIST / WRIST / KNEE / SHOULDER / NECK
        ctx.Sc?.Start(target, StatusType.Jointbeat, val1: skillLevel, val2: flag, val3: (int)src.Id, 0, durationMs: 30_000, src);
    }
}
