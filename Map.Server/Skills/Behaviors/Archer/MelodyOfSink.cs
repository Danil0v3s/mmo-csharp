using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_MELODYOFSINK — Minstrel/Wanderer Melody of Sink. Manual port of
/// <c>rathena-fork/src/map/skills/archer/melodyofsink.cpp</c>.
/// Per-target SC apply at <c>5 + 5*lv + WM_LESSON %</c> (passive
/// lookup TODO).
/// </summary>
public sealed class MelodyOfSink : SkillImpl
{
    private readonly Random _rng;

    public MelodyOfSink() : base(SkillIds.WM_MELODYOFSINK) => _rng = Random.Shared;

    public MelodyOfSink(Random? rng = null) : base(SkillIds.WM_MELODYOFSINK) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5 + 5 * skillLevel)
        {
            ctx.Sc?.Start(target, StatusType.Melodyofsink, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        }
    }
}
