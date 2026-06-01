using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.Magic"/> for skills with NO plugin —
/// rolls MATK in [min, max], scales by the per-level <see cref="SkillDefinition.DamageRate"/>
/// ratio, applies the element table + renewal Mdef formula. SKILL-05: magic
/// plugins don't override the ratio today, so this stays the fallback ratio
/// source; if dispatch leaks a plugin skill here, log it (a future magic ratio
/// override belongs on the plugin, not this column).
/// </summary>
public sealed class MagicSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.Magic;

    private readonly IDamageService _damage;
    private readonly Random _rng;
    private readonly Behaviors.SkillBehaviorRegistry? _behaviors;
    private readonly ILogger<MagicSkillResolver>? _logger;

    public MagicSkillResolver(
        IDamageService damage,
        Random? rng = null,
        Behaviors.SkillBehaviorRegistry? behaviors = null,
        ILogger<MagicSkillResolver>? logger = null)
    {
        _damage = damage;
        _rng = rng ?? Random.Shared;
        _behaviors = behaviors;
        _logger = logger;
    }

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        if (_behaviors?.HasCustomImpl(def.Id) == true)
            _logger?.LogWarning(
                "MagicSkillResolver invoked for skill {Skill} which HAS a plugin — dispatch leaked (the plugin should own this cast).",
                def.Id);
        var s = source.Stats;
        var matk = s.MatkMax > s.MatkMin
            ? _rng.Next(s.MatkMin, s.MatkMax + 1)
            : s.MatkMin;

        var rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        long dmg = matk * rate / 100;
        dmg = dmg * ElementTable.GetRate(def.Element, target.Stats.DefenseElement, target.Stats.ElementLevel) / 100;

        // Renewal Mdef: damage * (4000+mdef)/(4000+10*mdef) - mdef2
        var mdef1 = target.Stats.Mdef;
        var mdef2 = target.Stats.Mdef2;
        if (mdef1 == -400) mdef1 = -399;
        dmg = dmg * (4000L + mdef1) / (4000L + 10L * mdef1) - mdef2;
        if (dmg < 1) dmg = 1;

        _damage.ApplyDamage(target, (int)Math.Clamp(dmg, 0, int.MaxValue), source);
    }
}
