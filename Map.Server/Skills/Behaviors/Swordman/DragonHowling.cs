using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_DRAGONHOWLING — Rune Knight Dragon Howling. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/dragonhowling.cpp</c>.
/// Inflicts SC_FEAR at <c>50 + 6*lv</c>% on enemies in the splash.
/// Splash dispatch is TODO; for now we apply to the named target.
/// </summary>
public sealed class DragonHowling : SkillImpl
{
    private readonly Random _rng;

    public DragonHowling() : base(SkillIds.RK_DRAGONHOWLING) => _rng = Random.Shared;

    public DragonHowling(Random? rng = null) : base(SkillIds.RK_DRAGONHOWLING)
        => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var rate = 50 + 6 * skillLevel;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Fear, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
