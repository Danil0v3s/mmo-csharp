using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_SKYNETBLOW — Sura Sky Net Blow. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/skynetblow.cpp</c>.
/// Ratio <c>+(-100 + 200*lv) + AGI/6</c>; broadcasts then splash hits.
/// </summary>
public sealed class SkyNetBlow : RecursiveDamageSplashSkillImpl
{
    public SkyNetBlow() : base(SkillIds.SR_SKYNETBLOW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 200*lv + AGI/6;  RE_LVL_DMOD(100);
        return baseRatio + (-100 + 200 * skillLevel) + src.Stats.Agi / 6;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, target, skillLevel, ctx);
    }
}
