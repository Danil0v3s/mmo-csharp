using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.Weapon"/> — runs the standard
/// melee swing through <see cref="IBattleCalculator"/> and scales by
/// <see cref="SkillDefinition.DamageRate"/> per-level.
/// </summary>
public sealed class WeaponSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.Weapon;

    private readonly IBattleCalculator _battle;
    private readonly IDamageService _damage;

    public WeaponSkillResolver(IBattleCalculator battle, IDamageService damage)
    {
        _battle = battle;
        _damage = damage;
    }

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        var swing = _battle.CalcWeaponAttack(source, target);
        var rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        var scaled = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        _damage.ApplyDamage(target, scaled, source);
    }
}
