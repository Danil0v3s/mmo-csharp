using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// Warlock spellbook stack — rAthena <c>skill_spellbook</c>
/// (skill.cpp:~14855). Manages the per-caster ring of memorized
/// spells held in <c>SC_SPELLBOOK1..6 + SC_MAXSPELLBOOK</c> with
/// the running point total on <c>SC_FREEZE_SP</c>.Val2. Each slot:
///   Val1 = skill id, Val2 = skill lv, Val3 = preserve points
/// Consumption (<c>WL_RELEASE</c> lv 1) pops the newest slot and
/// decrements the FreezeSp running total.
/// </summary>
internal static class WarlockSpellbookHelpers
{
    /// <summary>The seven spellbook slots in rAthena's declaration order
    /// (Spellbook1 is the oldest slot, Maxspellbook the newest).</summary>
    public static readonly StatusType[] Slots =
    {
        StatusType.Spellbook1,
        StatusType.Spellbook2,
        StatusType.Spellbook3,
        StatusType.Spellbook4,
        StatusType.Spellbook5,
        StatusType.Spellbook6,
        StatusType.Maxspellbook,
    };

    /// <summary>
    /// rAthena <c>skill_spellbook</c> push branch. Tries to seat the
    /// (skillId, lv, points) into the first free slot. Returns true
    /// on success; false when every slot is full (rAthena clif_skill_fail
    /// USESKILL_FAIL_SPELLBOOK_READING).
    ///
    /// <para>Updates <c>SC_FREEZE_SP.Val2</c> as the running point
    /// total; starts SC_FREEZE_SP when absent.</para>
    /// </summary>
    public static bool PushSpell(PlayerEntity caster, ushort skillId, byte skillLv, int points,
        SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return false;
        // Find first free slot (rAthena scans Spellbook1..Maxspellbook).
        var freeIdx = -1;
        for (var i = 0; i < Slots.Length; i++)
        {
            if (ctx.Sc.Get(caster, Slots[i]) == null) { freeIdx = i; break; }
        }
        if (freeIdx < 0) return false;

        // Update / start FreezeSp running total.
        var freeze = ctx.Sc.Get(caster, StatusType.FreezeSp);
        if (freeze == null)
        {
            ctx.Sc.Start(caster, StatusType.FreezeSp, val1: 0, val2: points, 0, 0,
                durationMs: int.MaxValue, caster);
        }
        else
        {
            // Re-start to bump Val2 (no in-place mutator on the SC API).
            var newTotal = freeze.Val2 + points;
            ctx.Sc.End(caster, StatusType.FreezeSp);
            ctx.Sc.Start(caster, StatusType.FreezeSp, val1: 0, val2: newTotal, 0, 0,
                durationMs: int.MaxValue, caster);
        }
        // Seat the spell.
        ctx.Sc.Start(caster, Slots[freeIdx], val1: skillId, val2: skillLv, val3: points, 0,
            durationMs: int.MaxValue, caster);
        return true;
    }

    /// <summary>
    /// rAthena <c>WL_RELEASE</c> lv 1 pop branch. Returns the newest
    /// memorized spell (highest slot index), ends that slot's SC,
    /// and decrements <c>SC_FREEZE_SP.Val2</c> by the slot's points.
    /// Ends <c>SC_FREEZE_SP</c> outright if no slots remain.
    /// Returns null when the stack is empty.
    /// </summary>
    public static (ushort skillId, byte skillLv)? ConsumeNewest(PlayerEntity caster, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return null;
        // Walk slots high → low so we pop the latest memorize.
        for (var i = Slots.Length - 1; i >= 0; i--)
        {
            var entry = ctx.Sc.Get(caster, Slots[i]);
            if (entry == null) continue;
            var skillId = (ushort)entry.Val1;
            var skillLv = (byte)entry.Val2;
            var points = entry.Val3;
            ctx.Sc.End(caster, Slots[i]);
            // Adjust freeze-sp running total.
            var freeze = ctx.Sc.Get(caster, StatusType.FreezeSp);
            if (freeze != null)
            {
                var newTotal = Math.Max(0, freeze.Val2 - points);
                ctx.Sc.End(caster, StatusType.FreezeSp);
                if (newTotal > 0)
                {
                    ctx.Sc.Start(caster, StatusType.FreezeSp, val1: 0, val2: newTotal, 0, 0,
                        durationMs: int.MaxValue, caster);
                }
            }
            return (skillId, skillLv);
        }
        return null;
    }

    /// <summary>True when at least one spellbook slot is occupied.</summary>
    public static bool HasMemorized(PlayerEntity caster, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return false;
        foreach (var slot in Slots)
            if (ctx.Sc.Get(caster, slot) != null) return true;
        return false;
    }
}
