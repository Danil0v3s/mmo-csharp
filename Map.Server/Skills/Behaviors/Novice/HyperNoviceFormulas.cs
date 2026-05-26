using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// Shared Hyper Novice skill-ratio amplifiers ported from
/// <c>rathena-fork/src/map/skills/novice/*.cpp</c>. Centralises the
/// HN_SELFSTUDY_SOCERY / HN_SELFSTUDY_TATICS mastery boosts and the
/// SC_RULEBREAK trailer that every HN combat skill applies.
/// </summary>
internal static class HyperNoviceFormulas
{
    /// <summary>SOCERY mastery: +<paramref name="perLevel"/> · masteryLv · skillLv,
    /// then <c>·(100 + masteryLv · ampMul)/100</c> amplifier on the post-base
    /// ratio. <paramref name="ampMul"/> is 1 for most skills, 2 for
    /// Napalm Vulcan Strike (rAthena multiplies the amp by 2).</summary>
    public static int ApplySoceryBoost(int ratio, Entity src, ushort skillLevel, int perLevel, SkillBehaviorContext ctx, int ampMul = 1)
    {
        int mastery = ctx.PlayerSkill != null && src is PlayerEntity pc
            ? ctx.PlayerSkill.CheckSkill(pc, SkillIds.HN_SELFSTUDY_SOCERY) : 0;
        if (mastery <= 0) return ratio;
        ratio += mastery * perLevel * skillLevel;
        ratio += ratio * mastery * ampMul / 100;
        return ratio;
    }

    /// <summary>TATICS mastery: +<paramref name="perLevel"/> · masteryLv · skillLv.
    /// No post-base amplifier; TATICS is just the additive flat per-level.</summary>
    public static int ApplyTaticsBoost(int ratio, Entity src, ushort skillLevel, int perLevel, SkillBehaviorContext ctx)
    {
        int mastery = ctx.PlayerSkill != null && src is PlayerEntity pc
            ? ctx.PlayerSkill.CheckSkill(pc, SkillIds.HN_SELFSTUDY_TATICS) : 0;
        return ratio + mastery * perLevel * skillLevel;
    }

    /// <summary>SC_RULEBREAK boost — multiplies the ratio by
    /// (100 + <paramref name="pct"/>) / 100 when the caster carries
    /// SC_RULEBREAK. Per-skill the pct is 70 (most), 50 (Meteor Storm),
    /// or 40 (Napalm Vulcan Strike).</summary>
    public static int ApplyRuleBreakBoost(int ratio, Entity src, int pct, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(src, StatusType.Rulebreak) != null)
            ratio += ratio * pct / 100;
        return ratio;
    }
}
