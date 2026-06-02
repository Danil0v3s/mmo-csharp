using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Status change (buff/debuff) engine. Port of rAthena
/// <c>status_change_start</c> / <c>status_change_end</c> /
/// <c>status_change_timer</c> (status.cpp:9851 / 13732 / ~13732). First
/// slice supports a handful of common SCs (Poison DoT, Blessing,
/// Increase Agi, Heal-over-time, generic ATK% boost); per-SC handlers
/// plug in via <see cref="StatusEffectRegistry"/>.
/// </summary>
public interface IStatusChangeService
{
    /// <summary>
    /// Apply <paramref name="type"/> to <paramref name="target"/> for
    /// <paramref name="durationMs"/> ms. Refresh-on-restart: an existing
    /// SC of the same type is replaced (rAthena's default stacking rule
    /// for non-stackable SCs). Returns the new <see cref="StatusChange"/>
    /// or null if the application was rejected (e.g. immune mob).
    /// </summary>
    /// <summary>
    /// Apply <paramref name="type"/> to <paramref name="target"/>. Pass
    /// the current game tick in <paramref name="nowTick"/> so tests
    /// (and any deterministic-time caller) share a clock with the
    /// engine's <see cref="Tick"/> pump.
    /// </summary>
    StatusChange? Start(
        Entity target,
        StatusType type,
        int val1,
        int val2,
        int val3,
        int val4,
        int durationMs,
        Entity? source = null,
        long nowTick = long.MinValue);

    /// <summary>
    /// SKILL-01 — rate-aware apply. Mirrors rAthena <c>status_change_start</c>:
    /// the caller passes the RAW skill chance in <paramref name="rate"/>
    /// (1/100-% units, 10000 = guaranteed), and the engine runs it through
    /// <see cref="GetScDef"/> (stat resist + level-diff + boss/MVP immunity),
    /// rolls the (possibly reduced) chance, and on success applies the SC with
    /// the (possibly reduced) duration. Returns null when resisted/rolled-out.
    /// Use <paramref name="flag"/> (<see cref="ScStartFlag.NoRateDef"/> /
    /// <see cref="ScStartFlag.NoTickDef"/> / <see cref="ScStartFlag.NoAvoid"/>)
    /// to bypass resistance for guaranteed (self-buff) applies.
    /// </summary>
    StatusChange? Start(
        Entity target,
        StatusType type,
        int rate,
        int val1,
        int val2,
        int val3,
        int val4,
        int durationMs,
        Entity? source = null,
        ScStartFlag flag = ScStartFlag.None,
        long nowTick = long.MinValue)
        // Default for non-engine implementations (test doubles / recorders):
        // apply guaranteed via the no-rate path. The real StatusChangeService
        // overrides this with the full resist + roll pipeline.
        => Start(target, type, val1, val2, val3, val4, durationMs, source, nowTick);

    /// <summary>
    /// SKILL-01 — port of rAthena <c>status_get_sc_def</c> (status.cpp:9350,
    /// renewal branch). Returns the resisted landing <c>rate</c> (1/100-%)
    /// and the reduced <c>durationMs</c> for <paramref name="type"/> against
    /// <paramref name="target"/>'s battle stats, given the raw
    /// <paramref name="rate"/>/<paramref name="durationMs"/>. A resisted rate
    /// of 0 means the SC cannot land (immune). Does NOT roll — the caller
    /// (<see cref="Start"/>) rolls so the math is unit-testable.
    /// </summary>
    (int resistedRate, int reducedDurationMs) GetScDef(
        Entity? src, Entity target, StatusType type, int rate, int durationMs, ScStartFlag flag)
        // Default: no resistance (test doubles). Real engine overrides.
        => (rate, durationMs);

    /// <summary>Remove an active SC. No-op if not present.</summary>
    bool End(Entity target, StatusType type);

    /// <summary>Lookup; returns null if not active.</summary>
    StatusChange? Get(Entity target, StatusType type);

    /// <summary>
    /// COMBAT-33 — re-apply every active SC's DERIVED-stat contribution onto
    /// <paramref name="target"/>'s freshly-rebuilt <c>BattleStats</c>. Called by
    /// <c>StatusCalcService.CalcPc</c> after <c>CalcMisc</c> + the equip fold
    /// zero/recompute the derived fields, so SC mods like Angelus (+Def2),
    /// Provoke (Batk%/Def%) and the generated SCB_* set survive a recalc instead
    /// of being wiped. Mirrors rAthena re-folding every SCB_* contribution in
    /// <c>status_calc_pc_</c>. Default no-op so presence-only test doubles need
    /// no change; the real <c>StatusChangeService</c> overrides it.
    /// </summary>
    void ReapplyDerivedStatMods(Entity target) { }

    /// <summary>
    /// Tick all active SCs across all entities — expires elapsed ones,
    /// fires periodic effects (Poison DoT etc.) per their PeriodMs.
    /// Pumped from the map game loop.
    /// </summary>
    void Tick(long nowTick);

    /// <summary>
    /// ST.1 — rAthena <c>status_change_clear</c> (status.cpp:11497).
    /// Wipes every active SC on <paramref name="target"/>. <paramref name="type"/>
    /// mirrors rAthena semantics: 0 = leave permanent SCs alone,
    /// 1 = include them (used on PC death / disconnect), 2 = death-
    /// cleanup variant. Returns the number of SCs cleared.
    /// </summary>
    int ClearAll(Entity target, byte type = 0);

    /// <summary>
    /// ST.1 — rAthena <c>status_change_clear_buffs</c> (status.cpp:11616).
    /// Selectively wipes SCs whose handler <see cref="StatusEffectHandler.Flags"/>
    /// match the requested <see cref="SccbFlag"/> mask. Refresh skill,
    /// Dispell, Banishing Buster, Luxanima, Hermode all flow through here.
    /// </summary>
    int ClearBuffs(Entity target, SccbFlag flag);

    /// <summary>
    /// ST.1 — rAthena <c>status_change_clear_onChangeMap</c>
    /// (status.cpp:11848). Removes SCs flagged
    /// <see cref="ScfFlag.RemoveOnChangeMap"/>; leaves WoE-persistent /
    /// equipment-buff SCs intact. Called from the warp + map-change paths.
    /// </summary>
    int ClearOnChangeMap(Entity target);

    /// <summary>
    /// ST.1 — rAthena <c>status_change_clear_onLogout</c>. Removes SCs
    /// flagged <see cref="ScfFlag.RemoveOnLogout"/>. WoE god-item SCs
    /// and permanent equipment buffs survive.
    /// </summary>
    int ClearOnLogout(Entity target);

    /// <summary>
    /// ST.1 — rAthena <c>status_change_spread</c> (status.cpp:12188).
    /// For every active SC on <paramref name="source"/> whose handler
    /// is flagged <see cref="ScfFlag.SpreadEffect"/>, re-apply it to
    /// <paramref name="target"/>. Returns the number propagated.
    /// </summary>
    int Spread(Entity source, Entity target);

    /// <summary>
    /// ST.1 — rAthena <c>status_get_sc_max</c>. Stackable-buff cap.
    /// For non-stackable SCs returns 1.
    /// </summary>
    int GetMaxStacks(StatusType type);

    /// <summary>
    /// ST.1 — rAthena <c>status_change_isDisabledOnMap</c>
    /// (status.cpp:11992). Returns true when the map has a
    /// <c>nostatus</c> mapflag covering this SC. Wires through
    /// <c>IMapFlagService</c> when registered; returns false otherwise
    /// (parity with rAthena's default).
    /// </summary>
    bool IsDisabledOnMap(uint mapId, StatusType type);

    /// <summary>
    /// ST.7 — rAthena <c>status_change_refresh</c>
    /// (status.cpp:12222). Re-applies SCs whose duration / stat mod
    /// depends on the entity's current equipment (notably the
    /// weapon-element family: Fireweapon / Earthweapon / Windweapon /
    /// Waterweapon — when the PC swaps weapons the SC should drop
    /// + re-attach with the new weapon's element). Called from
    /// <c>IPlayerWeaponService.ChangeWeapon</c>. Returns the number
    /// of SCs refreshed.
    /// </summary>
    int Refresh(Entity target);
}
