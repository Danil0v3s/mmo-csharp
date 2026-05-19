using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Mirror of rAthena's <c>pc_bonus_script</c> / <c>pc_addautobonus</c>
/// family (pc.cpp:3650-3850) — temporary stat bonuses sourced from
/// item scripts. Two kinds:
///
/// <list type="bullet">
///   <item><b>bonus_script</b> — fixed-duration stat block applied
///         once (e.g. consumable that grants +20 STR for 5 min).</item>
///   <item><b>autobonus</b> — triggered bonus that re-fires its
///         <c>other_script</c> on hit / when hit / on skill use
///         until duration elapses (e.g. card "5% chance to cast
///         level 10 Heal on attack").</item>
/// </list>
///
/// Until the full script engine is wired, these methods record the
/// pending bonus on the PC and emit a log entry so the call site is
/// visible. Stat aggregation lands when the bonus-script runtime
/// joins <see cref="StatusCalcService"/>.
/// </summary>
public interface IPlayerBonusService
{
    /// <summary>
    /// rAthena <c>pc_bonus_script</c> — push a temporary script
    /// bonus. Returns true if the slot was open. Bonus persists
    /// across logout if <paramref name="persistent"/> is true.
    /// </summary>
    bool AddBonusScript(PlayerEntity pc, string script, int durationMs, ushort iconType, bool persistent);

    /// <summary>
    /// rAthena <c>pc_bonus_script_clear</c> — remove every bonus
    /// script matching <paramref name="flag"/> (BSF_REM_* filter).
    /// </summary>
    void ClearBonusScripts(PlayerEntity pc, int flag);

    /// <summary>
    /// rAthena <c>pc_addautobonus</c> (pc.cpp:3699) — register an
    /// auto-cast bonus. <paramref name="trigger"/> picks the event
    /// (on hit / when hit / on skill use).
    /// </summary>
    bool AddAutobonus(PlayerEntity pc, AutobonusTrigger trigger, string script, int rate, int durationMs, ushort flag);

    /// <summary>
    /// rAthena <c>pc_delautobonus</c> — clear a slot, optionally
    /// restoring the bonus to its original armor (when item unequips).
    /// </summary>
    void DelAutobonus(PlayerEntity pc, AutobonusTrigger trigger, bool restore);

    /// <summary>
    /// rAthena <c>pc_exeautobonus</c> — invoked from the combat /
    /// skill path when the trigger condition fires.
    /// </summary>
    void ExecuteAutobonus(PlayerEntity pc, AutobonusTrigger trigger);
}

/// <summary>Mirror of rAthena autobonus types (status.hpp).</summary>
public enum AutobonusTrigger
{
    /// <summary>Triggered when this PC lands a hit (pc_bonus2 bAutoSpell).</summary>
    OnHit,
    /// <summary>Triggered when this PC is hit (pc_bonus2 bAutoSpellWhenHit).</summary>
    WhenHit,
    /// <summary>Triggered when this PC casts the named skill.</summary>
    OnSkill,
}
