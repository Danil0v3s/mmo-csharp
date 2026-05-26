using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// Shared Shaman-family ratio amplifiers. Ported from
/// <c>rathena-fork/src/map/skills/summoner/*.cpp</c>. Centralises:
/// <list type="bullet">
///   <item>SH_MYSTICAL_CREATURE_MASTERY flat bonus (per-skill perLevel).</item>
///   <item>Spirit Communion bonus — gated by either the matching
///         SH_COMMUNE_WITH_* skill being learned, or by SC_TEMPORARY_COMMUNION
///         being active. Adds a fixed bonus + extra mastery bonus.</item>
/// </list>
/// </summary>
internal static class ShamanFormulas
{
    public enum CommuneSpirit { HyunRok, ChulHo, KiSul }

    /// <summary>Mystical Creature Mastery flat: <c>+masteryLv · perLevel</c>.</summary>
    public static int ApplyMasteryFlat(int ratio, Entity src, int perLevel, SkillBehaviorContext ctx)
    {
        int mastery = ctx.PlayerSkill != null && src is PlayerEntity pc
            ? ctx.PlayerSkill.CheckSkill(pc, SkillIds.SH_MYSTICAL_CREATURE_MASTERY) : 0;
        return ratio + mastery * perLevel;
    }

    /// <summary>Spirit communion bonus: <c>+base + lvScale·skillLv + masteryExtra·mastery</c>.
    /// Gates on the relevant SH_COMMUNE_WITH_* skill being known, OR SC_TEMPORARY_COMMUNION
    /// being active.</summary>
    public static int ApplyCommuneBoost(
        int ratio, Entity src, ushort skillLevel, CommuneSpirit spirit,
        int flatBase, int lvScale, int masteryExtra, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return ratio;
        bool communeActive =
            (ctx.Sc?.Get(src, StatusType.TemporaryCommunion) != null)
            || (ctx.PlayerSkill?.CheckSkill(pc, spirit switch
            {
                CommuneSpirit.HyunRok => SkillIds.SH_COMMUNE_WITH_HYUN_ROK,
                CommuneSpirit.ChulHo  => SkillIds.SH_COMMUNE_WITH_CHUL_HO,
                _                     => SkillIds.SH_COMMUNE_WITH_KI_SUL,
            }) ?? 0) > 0;
        if (!communeActive) return ratio;
        ratio += flatBase + lvScale * skillLevel;
        int mastery = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.SH_MYSTICAL_CREATURE_MASTERY) ?? 0;
        ratio += mastery * masteryExtra;
        return ratio;
    }
}
