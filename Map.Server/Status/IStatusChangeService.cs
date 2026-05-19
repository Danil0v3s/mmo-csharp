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

    /// <summary>Remove an active SC. No-op if not present.</summary>
    bool End(Entity target, StatusType type);

    /// <summary>Lookup; returns null if not active.</summary>
    StatusChange? Get(Entity target, StatusType type);

    /// <summary>
    /// Tick all active SCs across all entities — expires elapsed ones,
    /// fires periodic effects (Poison DoT etc.) per their PeriodMs.
    /// Pumped from the map game loop.
    /// </summary>
    void Tick(long nowTick);
}
