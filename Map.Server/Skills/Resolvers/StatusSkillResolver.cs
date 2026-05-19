using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.None"/> (status-only) — applies
/// the skill's bound SC for its per-level duration.
/// </summary>
public sealed class StatusSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.None;

    private readonly IStatusChangeService _scService;

    public StatusSkillResolver(IStatusChangeService scService) { _scService = scService; }

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        if (def.StatusType == StatusType.None) return;
        var duration = def.StatusDurationMs.Length > lvl ? def.StatusDurationMs[lvl] : 0;
        var val1 = def.EffectAmount.Length > lvl ? def.EffectAmount[lvl] : lvl;
        _scService.Start(target, def.StatusType, val1, 0, 0, 0, duration, source);
    }
}
