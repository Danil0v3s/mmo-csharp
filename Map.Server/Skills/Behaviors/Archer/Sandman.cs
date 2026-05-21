using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SANDMAN — Hunter Sandman trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/sandman.cpp</c>.
/// Trap + on-hit SC_SLEEP at <c>10*lv + 40 %</c>.
/// </summary>
public sealed class Sandman : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public Sandman() : base(SkillIds.HT_SANDMAN) => _rng = Random.Shared;

    public Sandman(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.HT_SANDMAN)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10 * skillLevel + 40)
            ctx.Sc?.Start(target, StatusType.Sleep, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
