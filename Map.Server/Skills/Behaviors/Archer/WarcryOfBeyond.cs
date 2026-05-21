using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_BEYOND_OF_WARCRY — Minstrel/Wanderer Warcry of Beyond. Manual
/// port of <c>rathena-fork/src/map/skills/archer/warcryofbeyond.cpp</c>.
/// Per-target SC apply at <c>12 + 3*lv + WM_LESSON %</c> (passive
/// scale TODO).
/// </summary>
public sealed class WarcryOfBeyond : SkillImpl
{
    private readonly Random _rng;

    public WarcryOfBeyond() : base(SkillIds.WM_BEYOND_OF_WARCRY) => _rng = Random.Shared;

    public WarcryOfBeyond(Random? rng = null) : base(SkillIds.WM_BEYOND_OF_WARCRY) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 12 + 3 * skillLevel)
        {
            ctx.Sc?.Start(target, StatusType.Beyondofwarcry, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        }
    }
}
