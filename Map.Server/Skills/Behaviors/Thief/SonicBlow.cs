using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_SONICBLOW — Sonic Blow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/sonicblow.cpp</c>.
/// Renewal: <c>+100 + 100*lv</c> ratio, +50% if target HP &lt; 50%.
/// Renewal hit-rate booster: AS_SONICACCEL learned → +90% hit.
/// Stun chance: <c>4*lv + 20</c>% under SC_SPIRIT + SL_ASSASIN link
/// (and not on GVG / BG maps), else <c>2*lv + 10</c>%.
/// </summary>
public sealed class SonicBlow : WeaponSkillImpl
{
    public SonicBlow() : base(SkillIds.AS_SONICBLOW) { }

    // COMBAT-39 — AS_SONICBLOW HitCount (-8 → 8 hits on the wire) now comes from
    // the SkillHitCounts table via the WeaponSkillImpl.GetMultiHitCount default, so
    // the explicit override is no longer needed.

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 100 + 100 * skillLevel;
        if (target is PlayerEntity p && p.Hp < p.MaxHp / 2)
            ratio += ratio / 2;
        else if (target is MobEntity m && m.Hp < m.MaxHp / 2)
            ratio += ratio / 2;
        return ratio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena renewal: pc_checkskill(sd, AS_SONICACCEL) > 0 →
        // hit_rate += hit_rate * 90 / 100. We read learned level via
        // PlayerEntity.LearnedSkills since ModifyHitRate has no ctx.
        if (src is PlayerEntity pc && pc.LearnedSkills.TryGetValue(SkillIds.AS_SONICACCEL, out var accel) && accel > 0)
            hitRate += (short)(hitRate * 90 / 100);
        return hitRate;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: SC_SPIRIT + val2 == SL_ASSASIN doubles the stun
        // chance (outside GVG / BG maps). We approximate the link
        // gate by reading SC_SPIRIT on the caster; Val2 carries the
        // linked job id at apply time.
        var spirit = ctx.Sc?.Get(src, StatusType.Spirit);
        var linkBonus = spirit != null && spirit.Val2 == SkillIds.SL_ASSASIN;
        var rate = linkBonus ? 4 * skillLevel + 20 : 2 * skillLevel + 10;
        if (System.Random.Shared.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0,
                durationMs: 5_000, src);
    }
}
