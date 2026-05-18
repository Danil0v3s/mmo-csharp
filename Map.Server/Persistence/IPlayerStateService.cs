using Map.Server.Session;

namespace Map.Server.Persistence;

/// <summary>
/// Persistence boundary between the live map session and the DB. Both core
/// character state (zeny, hp, sp, levels, …) and the three variable-register
/// scopes (perm / account / accountGlobal) flow through here.
///
/// Map.Server writes directly to the DB via Core.Database repositories /
/// DbContext — bypassing the char-server IPC for these fields. That trades
/// a bit of strict-rAthena-parity (rAthena routes saves through char) for
/// a much simpler implementation; char-server doesn't hold in-memory state
/// for the rows we touch, so concurrent-write races aren't realistic here.
/// </summary>
public interface IPlayerStateService
{
    /// <summary>
    /// Load the three persistent var-reg scopes for the player attached to
    /// <paramref name="session"/>, stash them on <see cref="MapSessionData.VarRegs"/>.
    /// Called by the connect flow after <see cref="MapSessionData.CharacterData"/>
    /// is hydrated.
    /// </summary>
    Task LoadAsync(MapSessionData session, CancellationToken ct = default);

    /// <summary>
    /// Save the session's current core state + dirty var-regs. Autosave
    /// calls this periodically, the disconnect cleanup calls it with
    /// <paramref name="finalSave"/> true.
    /// </summary>
    Task SaveAsync(MapSessionData session, bool finalSave, CancellationToken ct = default);
}
