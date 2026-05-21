using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_ANALYZE — Mechanic Analyze. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/analyze.cpp</c>.
/// Applies SC_ANALYZE at <c>30 + 12*lv %</c>.
/// </summary>
public sealed class Analyze : SkillImpl
{
    private readonly Random _rng;

    public Analyze() : base(SkillIds.NC_ANALYZE) => _rng = Random.Shared;

    public Analyze(Random? rng = null) : base(SkillIds.NC_ANALYZE) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 30 + 12 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Analyze, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
