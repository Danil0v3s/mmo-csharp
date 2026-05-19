using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.Magic"/> — rolls MATK in
/// [min, max], scales by per-level rate, applies the element table +
/// renewal Mdef formula, then deals damage.
/// </summary>
public sealed class MagicSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.Magic;

    private readonly IDamageService _damage;
    private readonly Random _rng;

    public MagicSkillResolver(IDamageService damage, Random? rng = null)
    {
        _damage = damage;
        _rng = rng ?? Random.Shared;
    }

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
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
