namespace Map.Server.Status;

/// <summary>
/// Flag set used by <see cref="IStatusChangeService.ClearBuffs"/>.
/// Mirrors rAthena <c>enum e_status_change_clear_buffs_flags</c>
/// (status.hpp:3133). Callers OR these together to select which SCs
/// to wipe — e.g. Refresh skill passes <c>Refresh</c> alone, Dispell
/// passes <c>Buffs | Debuffs</c>, Banishing Buster passes <c>Buffs</c>.
/// </summary>
[System.Flags]
public enum SccbFlag : long
{
    /// <summary>No-op.</summary>
    None = 0,
    /// <summary>Remove positive SCs (Blessing, IncreaseAgi, Concentration, …).</summary>
    Buffs = 0x01,
    /// <summary>Remove negative SCs (Stone, Bleeding, Curse, …).</summary>
    Debuffs = 0x02,
    /// <summary>Remove SCs flagged as cleared by Refresh skill (Stun, Sleep, Freeze, Stone, Bleeding, Burning, Confusion, Provoke, …).</summary>
    Refresh = 0x04,
    /// <summary>Alchemist Chemical Protection family — leaves protect-* SCs alone.</summary>
    ChemProtect = 0x08,
    /// <summary>Royal Guard's Luxanima — purges most player buffs.</summary>
    Luxanima = 0x10,
    /// <summary>Wand of Hermode purge (drops buffs + debuffs except permanent / WoE-specific).</summary>
    Hermode = 0x20,
}

/// <summary>
/// Per-SC behavior flags. Mirrors a subset of rAthena <c>SCF_*</c>
/// (status.hpp ~3050). Stored on <see cref="StatusEffectHandler.Flags"/>
/// so the engine can answer "is this a buff or a debuff", "does it
/// drop on map change", "does it spread", etc. without a giant
/// switch.
/// </summary>
[System.Flags]
public enum ScfFlag : long
{
    /// <summary>No flags.</summary>
    None = 0,
    /// <summary>Counted as a positive buff (visible to SCCB_BUFFS clear).</summary>
    Buff = 0x01,
    /// <summary>Counted as a negative debuff (visible to SCCB_DEBUFFS clear).</summary>
    Debuff = 0x02,
    /// <summary>Removed by Refresh skill (SCCB_REFRESH).</summary>
    RemoveOnRefresh = 0x04,
    /// <summary>Removed when the entity changes map (rAthena: dropped from <c>SCS_NOREMOVEONCHANGEMAP</c> protection).</summary>
    RemoveOnChangeMap = 0x08,
    /// <summary>Removed when the player logs out (most buffs do, WoE SCs don't).</summary>
    RemoveOnLogout = 0x10,
    /// <summary>Spreads to adjacent units on tick (Influenza, Burning per spread_rate).</summary>
    SpreadEffect = 0x20,
    /// <summary>Removed when the entity takes damage (Hiding, Cloaking, Sleep on hit).</summary>
    RemoveOnDamaged = 0x40,
    /// <summary>Boss-detect — Boss mobs are immune unless this flag is set on the SC.</summary>
    BossDetect = 0x80,
    /// <summary>Survives the Refresh skill explicitly (e.g. WoE God-item SCs).</summary>
    Permanent = 0x100,
}
