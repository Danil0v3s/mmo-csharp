using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_PICKYPECK — Summoner Picky Peck. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/pickypeck.cpp</c>.
/// Ratio <c>+(100 + 100*lv)</c>; doubles when target HP &lt; MaxHP/2;
/// SU_SPIRITOFLIFE adds HP-ratio multiplier (TODO — skilltree query).
/// </summary>
public sealed class PickyPeck : WeaponSkillImpl
{
    public PickyPeck() : base(SkillIds.SU_PICKYPECK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 100 + 100 * skillLevel;
        if (target.Stats.Hp < target.Stats.MaxHp / 2)
            ratio *= 2;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
