using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.AssassinCross;

/// <summary>
/// ASC_BREAKER — Assassin Cross Soul Breaker. Mirrors
/// <c>rathena-fork/src/map/skills/assassincross/soulbreaker.cpp</c>.
///
/// Hybrid physical + magic hit. Two stages: weapon-based at
/// (200 + 50*lv)% ATK + INT-scaling magic component. We
/// approximate as a single combined hit for the first port; the
/// rAthena dual-component split lands when DamageKind dual lands.
/// </summary>
public sealed class SoulBreaker : SkillImpl
{
    public SoulBreaker() : base(SkillIds.ASC_BREAKER) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var phys = (int)Math.Clamp(swing.Total * (200 + 50 * skillLevel) / 100, 0, int.MaxValue);
        // Magic component (INT-scaling, ghost element).
        var magic = src.Stats.IntStat * 4 + skillLevel * 50;
        ctx.Damage.ApplyDamage(target, Math.Max(1, phys + magic), src);
    }
}
