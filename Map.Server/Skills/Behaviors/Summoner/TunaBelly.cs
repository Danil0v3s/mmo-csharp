using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_TUNABELLY — Summoner Tuna Belly. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/tunabelly.cpp</c>.
/// Heals the target for <c>(2*lv - 1) * 10%</c> of MaxHP. Emperium / BG
/// targets are immune.
/// </summary>
public sealed class TunaBelly : SkillImpl
{
    public TunaBelly() : base(SkillIds.SU_TUNABELLY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: skip Emperium / BG class targets.
        if (target.Stats.Hp == target.Stats.MaxHp) return;
        var heal = ((2 * skillLevel - 1) * 10) * target.Stats.MaxHp / 100;
        if (target is PlayerEntity p)
            p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
        else if (target is MobEntity m)
            m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
    }
}
