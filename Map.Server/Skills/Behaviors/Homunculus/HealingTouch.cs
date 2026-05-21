using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HLIF_HEAL — Lif Healing Touch. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_healingtouch.cpp</c>.
/// Heals the target via skill_calc_heal. Kaite reflect + heal-exp gain
/// are TODO; we apply a baseline heal of MaxHP*5%.
/// </summary>
public sealed class HealingTouch : SkillImpl
{
    public HealingTouch() : base(SkillIds.HLIF_HEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var heal = target.Stats.MaxHp * (3 + skillLevel) / 100;
        if (target is PlayerEntity p)
            p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
        else if (target is MobEntity m)
            m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
