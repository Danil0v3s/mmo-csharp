using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_LULLABY_DEEPSLEEP — Minstrel/Wanderer Deep Sleep Lullaby. Manual
/// port of <c>rathena-fork/src/map/skills/archer/deepsleeplullaby.cpp</c>.
/// SC_DEEPSLEEP with level/INT-scaled duration. Splash TODO.
/// </summary>
public sealed class DeepSleepLullaby : SkillImpl
{
    public DeepSleepLullaby() : base(SkillIds.WM_LULLABY_DEEPSLEEP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var rate = 4 * skillLevel + src.Level / 15;
        if (Random.Shared.Next(100) < rate)
        {
            var duration = Math.Max(1000, 30_000 - (target.Stats.IntStat * 50 + target.Level * 50));
            ctx.Sc?.Start(target, StatusType.Deepsleep, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        }
    }
}
