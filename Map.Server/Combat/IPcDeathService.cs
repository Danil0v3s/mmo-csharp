using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Owns the player-death + respawn cycle. Port of rAthena
/// <c>pc_dead</c> (pc.cpp:9633) + <c>pc_respawn</c> (pc.cpp:9515).
///
/// MS3 first slice covers the renewal essentials: mark the PC dead so
/// combat / movement stops, apply the renewal death-penalty EXP loss,
/// keep the entity in the world (it's a corpse, the client needs the
/// vanish + restart dialog), and warp to the savepoint on respawn.
///
/// Out of scope for now (deferred to per-system slices):
/// - Devotion / Stellar Mark / Servant Sign cleanup
/// - Pet intimacy decay / homunculus auto-vapor
/// - Mercenary / elemental dispel
/// - PvP point loss
/// - Save-on-death char-server IPC (lifecycle sweep already handles
///   the normal flush — death just sets the right values first)
/// </summary>
public interface IPcDeathService
{
    /// <summary>Mark <paramref name="pc"/> dead. Idempotent.</summary>
    void OnPcDead(PlayerEntity pc, Entity? source);

    /// <summary>Handle the client's <c>CZ_RESTART(type=0)</c> respawn request.</summary>
    void Respawn(PlayerEntity pc);

    /// <summary>True while the PC is dead and awaiting respawn.</summary>
    bool IsDead(PlayerEntity pc);

    /// <summary>
    /// Record the PC's savepoint coordinates — read from
    /// <c>CharacterDataResponse.LastMap / LastX / LastY</c> at session
    /// enter. Used by <see cref="Respawn"/> to call <c>pc_setpos</c>.
    /// </summary>
    void SetSavepoint(int characterId, string mapName, short x, short y);

    /// <summary>
    /// rAthena <c>atcommand_load</c> primitive — warp to savepoint without
    /// requiring the caller to be dead. Returns false if no savepoint is
    /// recorded for this character.
    /// </summary>
    bool WarpToSavepoint(PlayerEntity pc);
}
