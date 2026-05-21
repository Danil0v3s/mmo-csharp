using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_DECAGI — Mercenary Decrease Agi. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_decreaseagi.cpp</c>.
/// Rate <c>50 + 3*lv + (BaseLv + INT)/5</c>%, duration from skill_db.
/// </summary>
public sealed class MercenaryDecreaseAgi : SkillImpl
{
    private readonly Random _rng;

    public MercenaryDecreaseAgi() : base(SkillIds.MER_DECAGI) => _rng = Random.Shared;

    public MercenaryDecreaseAgi(Random? rng = null) : base(SkillIds.MER_DECAGI)
        => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 50 + 3 * skillLevel + (src.Level + src.Stats.IntStat) / 5;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.DecreaseAgi, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
