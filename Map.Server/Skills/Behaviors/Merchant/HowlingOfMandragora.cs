using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_MANDRAGORA — Genetic Howling of Mandragora. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/howlingofmandragora.cpp</c>.
/// SC_MANDRAGORA at <c>max(10, 25 + 10*lv - (VIT+LUK)/5) %</c>;
/// on success drains <c>25 + 5*lv %</c> of target's max SP.
/// </summary>
public sealed class HowlingOfMandragora : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public HowlingOfMandragora() : base(SkillIds.GN_MANDRAGORA) => _rng = Random.Shared;

    public HowlingOfMandragora(IStatusOpsService? statusOps = null, Random? rng = null) : base(SkillIds.GN_MANDRAGORA)
    {
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity) return;
        var rate = Math.Max(10, 25 + 10 * skillLevel - (target.Stats.Vit + target.Stats.Luk) / 5);
        if (_rng.Next(100) < rate)
        {
            ctx.Sc?.Start(target, StatusType.Mandragora, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            _statusOps?.Zap(target, 0, target.Stats.MaxSp * (25 + 5 * skillLevel) / 100);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
