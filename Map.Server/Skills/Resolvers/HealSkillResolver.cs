using Map.Server.Entities;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.Heal"/> — restores HP using the
/// renewal heal formula. Mirrors rAthena <c>skill_calc_heal</c>.
/// </summary>
public sealed class HealSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.Heal;

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        var multiplier = def.EffectAmount.Length > lvl ? def.EffectAmount[lvl] : 0;
        var baseAmount = (source.Level + source.Stats.IntStat) / 8;
        var heal = Math.Max(1, baseAmount * multiplier);

        switch (target)
        {
            case PlayerEntity p:
                p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
                break;
            case MobEntity m:
                m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
                break;
        }
    }
}
