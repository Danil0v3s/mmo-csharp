using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillSideEffectService"/>. The heal formula is
/// real; autospell / break / strip are data-pending on the equip
/// catalog (we don't track break / strip status on inventory rows
/// yet) — the canonical entry points are here so callers don't drift.
/// </summary>
public sealed class SkillSideEffectService : ISkillSideEffectService
{
    private readonly ISkillDb _db;
    private readonly ILogger<SkillSideEffectService> _logger;

    public SkillSideEffectService(ISkillDb db, ILogger<SkillSideEffectService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public int CalcHeal(Entity caster, Entity target, ushort skillId, ushort skillLevel, bool heal)
    {
        // Renewal heal formula: amount = (BaseLv + Int) / 8 * (4 + lvl * 8) * (skill_db EffectAmount multiplier)
        // For the starter set we reuse SkillDefinition.EffectAmount as the
        // per-level multiplier (HealSkillResolver does the same).
        var def = _db.Get(skillId);
        if (def == null) return 0;
        var multiplier = def.EffectAmount.Length > skillLevel ? def.EffectAmount[skillLevel] : 0;
        if (multiplier <= 0) return 0;
        var baseAmount = (caster.Level + caster.Stats.IntStat) / 8;
        return Math.Max(1, baseAmount * multiplier);
    }

    public bool AutoSpell(Entity caster, ushort grantedSkillId)
    {
        // rAthena: Sage AutoSpell sets SC_AUTOSPELL; the trigger that
        // procs it lives in the on-hit chain. We surface the entry
        // point; SC_AUTOSPELL handling is data-pending on the SC table.
        _logger.LogDebug("skill_autospell: granted {Skill} to {Caster} (data-pending)", grantedSkillId, caster.Id);
        return true;
    }

    public bool BreakEquip(Entity src, Entity target, int equipMask, int rate)
    {
        // rAthena rolls a chance against `rate` and flips the
        // equip's `attribute` flag to broken. Requires the inventory
        // BrokenFlag column to be plumbed onto the equip table.
        _logger.LogDebug("skill_break_equip: mask=0x{Mask:X} rate={Rate} (data-pending)", equipMask, rate);
        return false;
    }

    public bool StripEquip(Entity src, Entity target, int equipMask, int durationMs)
    {
        // rAthena status_change_start with the equip-mask in val1.
        // Data-pending on SC_STRIPWEAPON / STRIPHELM / STRIPARMOR /
        // STRIPSHIELD entries in the SC table.
        _logger.LogDebug("skill_strip_equip: mask=0x{Mask:X} duration={Ms} (data-pending)", equipMask, durationMs);
        return false;
    }
}
