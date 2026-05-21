using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_ARRULLO — Sorcerer Arrullo. Manual port of
/// <c>rathena-fork/src/map/skills/mage/arrullo.cpp</c>.
///
/// <para>AOE deep-sleep. Per-victim rate:
/// <c>(15 + 5*lv) + caster_INT/5 + job_level/5 - target_INT/6 - target_LUK/10</c>.
/// Splash dispatch is TODO — the named target gets the roll.
/// job_level read falls back to 50 until exposed on Entity.</para>
/// </summary>
public sealed class Arrullo : SkillImpl
{
    private readonly Random _rng;

    public Arrullo() : base(SkillIds.SO_ARRULLO) => _rng = Random.Shared;

    public Arrullo(Random? rng = null) : base(SkillIds.SO_ARRULLO) => _rng = rng ?? Random.Shared;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: full splash dispatch via map_foreachinallarea.
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var jobLevel = 50;
        var rate = (15 + 5 * skillLevel)
            + src.Stats.IntStat / 5
            + jobLevel / 5
            - target.Stats.IntStat / 6
            - target.Stats.Luk / 10;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Deepsleep, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
