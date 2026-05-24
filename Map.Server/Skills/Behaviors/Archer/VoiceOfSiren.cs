using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_VOICEOFSIREN — Minstrel/Wanderer Voice of Siren. Manual port of
/// <c>rathena-fork/src/map/skills/archer/voiceofsiren.cpp</c>.
/// SC apply at <c>lv*6 + 25 %</c> (Voice Lesson + job_level bonuses
/// deferred).
/// </summary>
public sealed class VoiceOfSiren : SkillImpl
{
    private readonly Random _rng;

    public VoiceOfSiren() : base(SkillIds.WM_VOICEOFSIREN) => _rng = Random.Shared;

    public VoiceOfSiren(Random? rng = null) : base(SkillIds.WM_VOICEOFSIREN) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = skillLevel * 6 + 25;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Voiceofsiren, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
