using Map.Server.Combat;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.Weapon"/> for skills with NO plugin —
/// runs the standard melee swing and scales by
/// <see cref="SkillDefinition.DamageRate"/> per-level (the no-plugin fallback
/// ratio source). SKILL-05: a skill that HAS a
/// <see cref="Behaviors.WeaponSkillImpl"/> plugin must never get its ratio from
/// <c>DamageRate</c> — if dispatch leaks one here, defer to the plugin's single
/// ratio authority and log the leak.
/// </summary>
public sealed class WeaponSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.Weapon;

    private readonly IBattleCalculator _battle;
    private readonly IDamageService _damage;
    private readonly Behaviors.SkillBehaviorRegistry? _behaviors;
    private readonly ILogger<WeaponSkillResolver>? _logger;

    public WeaponSkillResolver(
        IBattleCalculator battle,
        IDamageService damage,
        Behaviors.SkillBehaviorRegistry? behaviors = null,
        ILogger<WeaponSkillResolver>? logger = null)
    {
        _battle = battle;
        _damage = damage;
        _behaviors = behaviors;
        _logger = logger;
    }

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        // COMBAT-78 — skill-aware swing only when a plugin owns the ratio (crit_atk_rate ÷200
        // deferred to ComputeSkillDamage); the DamageRate fallback keeps the basic ÷100 swing.
        var plugin = _behaviors?.Get(def.Id) as Behaviors.WeaponSkillImpl;
        var swing = _battle.CalcWeaponAttack(source, target, plugin != null ? def.Id : (ushort)0);

        // SKILL-05 — the plugin is the single ratio authority. Reaching this
        // resolver for a plugin skill means dispatch leaked (SkillCastService
        // routes to the plugin first); honor the plugin ratio anyway and warn
        // so the leak is visible rather than silently applying DamageRate.
        if (plugin != null)
        {
            _logger?.LogWarning(
                "WeaponSkillResolver invoked for skill {Skill} which HAS a plugin — dispatch leaked; using the plugin ratio, not DamageRate.",
                def.Id);
            var pluginDmg = plugin.ComputeSkillDamage(swing, source, target, lvl, ctx: null);
            _damage.ApplyDamage(target, pluginDmg, source);
            return;
        }

        var rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        var scaled = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        _damage.ApplyDamage(target, scaled, source);
    }
}
