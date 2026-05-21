using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_KI_SUL_WATER_SPRAYING — Shaman Ki Sul Water Spraying. Manual
/// port of <c>rathena-fork/src/map/skills/summoner/kisulwaterspraying.cpp</c>.
/// Heals <c>(500*lv + 5*INT + 100*MysticalCreatureMastery) * (100+CRT) * BaseLv / 10000</c>
/// HP on the target (no party splash here). Mastery / CRT plumbing TODO.
/// </summary>
public sealed class KisulWaterSpraying : SkillImpl
{
    public KisulWaterSpraying() : base(SkillIds.SH_KI_SUL_WATER_SPRAYING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var heal = (500 * skillLevel + src.Stats.IntStat * 5) * (100 + src.Stats.Con) * src.Level / 10_000;
        if (target is PlayerEntity p)
            p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
        else if (target is MobEntity m)
            m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
    }
}
