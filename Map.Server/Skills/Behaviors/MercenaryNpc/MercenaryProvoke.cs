using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_PROVOKE — Mercenary Provoke. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_provoke.cpp</c>.
/// Status-immune mobs and undead targets are skipped. Apply rate
/// <c>70 + 3*lv + (BaseLv - target.Lv)</c>%.
/// </summary>
public sealed class MercenaryProvoke : SkillImpl
{
    private readonly Random _rng;

    public MercenaryProvoke() : base(SkillIds.MER_PROVOKE) => _rng = Random.Shared;

    public MercenaryProvoke(Random? rng = null) : base(SkillIds.MER_PROVOKE)
        => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        if (target.Stats.DefenseElement == BattleElement.Undead) return;
        var rate = 70 + 3 * skillLevel + src.Level - target.Level;
        if (_rng.Next(100) < rate)
        {
            var durationMs = 30_000 - 1_000 * skillLevel;
            ctx.Sc?.Start(target, StatusType.Provoke, val1: skillLevel, 0, 0, 0, durationMs, src);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        }
    }
}
