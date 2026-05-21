using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_WINKCHARM — Dancer Wink of Charm. Manual port of
/// <c>rathena-fork/src/map/skills/archer/winkofcharm.cpp</c>.
/// Renewal: SC_CONFUSION + SC_HALLUCINATION 100 % on players.
/// On mobs: SC_WINKCHARM at <c>(src.Lv - tgt.Lv) + 40 %</c>.
/// </summary>
public sealed class WinkofCharm : SkillImpl
{
    private readonly Random _rng;

    public WinkofCharm() : base(SkillIds.DC_WINKCHARM) => _rng = Random.Shared;

    public WinkofCharm(Random? rng = null) : base(SkillIds.DC_WINKCHARM) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity)
        {
            ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            ctx.Sc?.Start(target, StatusType.Hallucination, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
        else if (target is MobEntity)
        {
            var rate = (src.Level - target.Level) + 40;
            if (_rng.Next(100) < rate)
                ctx.Sc?.Start(target, StatusType.Winkcharm, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
