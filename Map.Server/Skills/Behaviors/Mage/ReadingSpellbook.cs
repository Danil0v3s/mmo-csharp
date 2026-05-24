using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_READING_SB_READING — Warlock Reading Spellbook
/// (skill.cpp:WL_READING_SB_READING). Memorizes one Warlock spell
/// into the SC_SPELLBOOK1..6 + SC_MAXSPELLBOOK ring, accumulating
/// preserve points on SC_FREEZE_SP.
///
/// <para>rAthena reads the book item id (<c>ITEMID_WL_MB_SG + lv - 1</c>),
/// resolves it through <c>reading_spellbook_db</c> → (skill_id, points),
/// and seats it. We mirror this with a per-level static table covering
/// the canonical Warlock spell roster; the slot push + points-total
/// math runs through <see cref="WarlockSpellbookHelpers.PushSpell"/>.</para>
/// </summary>
public sealed class ReadingSpellbook : SkillImpl
{
    /// <summary>Per-level book→spell mapping (rAthena ITEMID_WL_MB_*
    /// catalog). Index = skillLevel - 1; tuple is (granted skill id,
    /// preserve points consumed from the FreezeSp pool).</summary>
    private static readonly (ushort skillId, byte points)[] PerLevel =
    {
        (SkillIds.WL_SOULEXPANSION, 3),
        (SkillIds.WL_FROSTMISTY, 3),
        (SkillIds.WL_JACKFROST, 3),
        (SkillIds.WL_DRAINLIFE, 2),
        (SkillIds.WL_CRIMSONROCK, 4),
        (SkillIds.WL_HELLINFERNO, 2),
        (SkillIds.WL_COMET, 5),
        (SkillIds.WL_CHAINLIGHTNING, 4),
        (SkillIds.WL_EARTHSTRAIN, 4),
        (SkillIds.WL_TETRAVORTEX, 5),
    };

    public ReadingSpellbook() : base(SkillIds.WL_READING_SB_READING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        if (skillLevel < 1 || skillLevel > PerLevel.Length)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.Skill);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var (spellId, points) = PerLevel[skillLevel - 1];
        // rathena-fork plugin only forwards to skill_spellbook —
        // the SC_SLEEP / SC_FREEZE_SP starts happen inside that helper,
        // not in the plugin's emit trace. PushSpell does the
        // observable mutation; the unknown-spell / cap-exceeded
        // branches return silently to keep parity-extractor in sync.
        var learned = ctx.PlayerSkill?.CheckSkill(sd, spellId) ?? 0;
        if (learned == 0) return;
        WarlockSpellbookHelpers.PushSpell(sd, spellId, (byte)learned, points, ctx);
    }
}
