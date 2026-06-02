using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillSideEffectService"/>. The heal formula is
/// real; autospell / break flag flips are deferred per PARITY-REMAINING.md §P2.2 — equip still pending
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
    private readonly IMapSessionRegistry? _sessions;
    private readonly Random _rng;

    public SkillSideEffectService(
        ISkillDb db,
        ILogger<SkillSideEffectService> logger,
        IStatusChangeService? sc = null,
        IMapSessionRegistry? sessions = null,
        Random? rng = null)
    {
        _db = db;
        _logger = logger;
        _sc = sc;
        _sessions = sessions;
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
        // point; SC_AUTOSPELL handling is deferred per PARITY-REMAINING.md §P2.2 — SC table still pending.
        _logger.LogDebug("skill_autospell: granted {Skill} to {Caster} (deferred per PARITY-REMAINING.md §P2.2)", grantedSkillId, caster.Id);
        return true;
    }

    public bool BreakEquip(Entity src, Entity target, int equipMask, int rate)
    {
        // rAthena: skill_break_equip (skill.cpp ~5856) rolls a single
        // chance against `rate` (units = 0.01% i.e. 10000 = 100%). On
        // success, picks the first equipped item whose slot intersects
        // `equipMask`, sets `attribute |= 1` (the "broken" flag), and
        // unequips it. PvE break chains expect this exact behavior so
        // the on-hit pipeline can stack break + dispel cleanly.
        if (target is not PlayerEntity pc) return false;
        if (rate <= 0) return false;
        // COMBAT-65 — unbreakable_equip mask (rAthena skill_break_equip, skill.cpp:2840:
        // `where &= ~sd->bonus.unbreakable_equip`): a slot the wearer flags unbreakable
        // is removed from the break mask, so it can never be picked below.
        equipMask = (int)((uint)equipMask & ~UnbreakableMask(pc));
        if (equipMask == 0) return false;
        if (_rng.Next(10000) >= rate) return false;

        var session = _sessions?.TryGet(pc);
        if (session?.Inventory is not { } inv) return false;
        foreach (var item in inv)
        {
            if (item.Amount == 0) continue;
            if ((item.Equip & (uint)equipMask) == 0) continue;
            // rAthena Attribute bit 0 = broken. Also clear the Equip
            // bits so EquipBonusAggregator's next recalc drops this
            // item from the live stat block.
            item.Attribute |= 1;
            item.Equip = 0;
            _logger.LogInformation(
                "skill_break_equip: pc={Char} broke item {ItemId} (slot mask 0x{Mask:X})",
                pc.CharacterId, item.NameId, equipMask);
            return true;
        }
        return false;
    }

    /// <summary>
    /// COMBAT-65 — the wearer's unbreakable-equip slot mask (rAthena
    /// <c>sd->bonus.unbreakable_equip</c>, set by <c>bUnbreakableWeapon</c> … per
    /// SP_UNBREAKABLE_*). Maps each flag to its EQP_* slot bit.
    /// </summary>
    internal static uint UnbreakableMask(PlayerEntity pc)
    {
        var ab = pc.EquipBonuses;
        uint m = 0;
        if (ab.UnbreakableWeapon)  m |= EquipBits.HandR;
        if (ab.UnbreakableShield)  m |= EquipBits.HandL;
        if (ab.UnbreakableArmor)   m |= EquipBits.Armor;
        if (ab.UnbreakableHelm)    m |= EquipBits.Helm;
        if (ab.UnbreakableShoes)   m |= EquipBits.Shoes;
        if (ab.UnbreakableGarment) m |= EquipBits.Garment;
        return m;
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
