using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillSideEffectService"/>. The heal formula is
/// real; autospell / break flag flips are data-pending on the equip
/// catalog (we don't track break / strip status on inventory rows
/// yet). T2.4b+ wires <see cref="StripEquip"/> to the SC table so
/// the strip duration is at least recorded; equip-side enforcement
/// rides on the SC presence flag.
/// </summary>
public sealed class SkillSideEffectService : ISkillSideEffectService
{
    private readonly ISkillDb _db;
    private readonly ILogger<SkillSideEffectService> _logger;
    private readonly IStatusChangeService? _sc;
    private readonly Random _rng;

    public SkillSideEffectService(
        ISkillDb db,
        ILogger<SkillSideEffectService> logger,
        IStatusChangeService? sc = null,
        Random? rng = null)
    {
        _db = db;
        _logger = logger;
        _sc = sc;
        _rng = rng ?? Random.Shared;
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

    /// <summary>
    /// rAthena <c>skill_strip_equip</c> (skill.cpp:5970) — Rogue's
    /// Strip family. Picks the correct SC per slot (Weapon / Shield /
    /// Armor / Helm) and attaches it for <paramref name="durationMs"/>.
    /// Multiple-bit masks attach one SC per slot.
    /// </summary>
    public bool StripEquip(Entity src, Entity target, int equipMask, int durationMs)
    {
        if (_sc == null || durationMs <= 0)
        {
            _logger.LogDebug("skill_strip_equip: mask=0x{Mask:X} duration={Ms} (sc-engine missing)", equipMask, durationMs);
            return false;
        }

        var anyApplied = false;
        var mask = (uint)equipMask;
        if ((mask & EquipBits.HandR) != 0 || (mask & EquipBits.HandL) != 0)
        {
            _sc.Start(target, StatusType.Stripweapon, val1: (int)mask, 0, 0, 0, durationMs, src);
            anyApplied = true;
        }
        if ((mask & EquipBits.HandL) != 0)
        {
            // Shield share the HandL bit; rAthena differentiates by
            // checking the actual equipped item kind. Until equip-kind
            // dispatch ports, fall back to attaching both SCs.
            _sc.Start(target, StatusType.Stripshield, val1: (int)mask, 0, 0, 0, durationMs, src);
            anyApplied = true;
        }
        if ((mask & EquipBits.Armor) != 0)
        {
            _sc.Start(target, StatusType.Striparmor, val1: (int)mask, 0, 0, 0, durationMs, src);
            anyApplied = true;
        }
        if ((mask & (EquipBits.HeadTop | EquipBits.HeadMid | EquipBits.HeadLow)) != 0)
        {
            _sc.Start(target, StatusType.Striphelm, val1: (int)mask, 0, 0, 0, durationMs, src);
            anyApplied = true;
        }

        if (anyApplied)
        {
            _logger.LogDebug("skill_strip_equip: mask=0x{Mask:X} duration={Ms} → SC attached", equipMask, durationMs);
        }
        return anyApplied;
    }
}
