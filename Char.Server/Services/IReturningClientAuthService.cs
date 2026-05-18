namespace Char.Server.Services;

/// <summary>
/// In-memory store of clients expected back for char-select. Populated by
/// the map server's <c>NotifyCharacterSelectAuthOk</c> IPC when a player
/// clicks "back to character select" (rAthena <c>chrif_charselectreq</c> /
/// <c>chmapif_parse_reqcharselect</c> → <c>auth_db</c> entry).
///
/// Consumed by <see cref="Handlers.ClientConnectHandler"/>: when the client
/// reconnects with <c>CH_REQ_TO_CONNECT</c>, an entry here bypasses the
/// login-server round-trip (whose <c>auth_node</c> for this account was
/// already consumed on the initial char→map handoff). One-shot — the entry
/// is removed on first consume.
/// </summary>
public interface IReturningClientAuthService
{
    /// <summary>
    /// Register that the client with these credentials is expected to
    /// reconnect for char-select. Overwrites any prior entry for the same
    /// account.
    ///
    /// Match key is <c>(accountId, loginId1, sex)</c>. <c>loginId2</c> is
    /// dropped because <c>CZ_WANT_TO_CONNECTION</c> doesn't carry it and
    /// our map server can't pass it through; the existing
    /// <c>MapAuthTicketService</c> already treats <c>loginId2 = 0</c> as
    /// "skip the check" and we follow that convention.
    /// </summary>
    void Allow(int accountId, int loginId1, byte sex);

    /// <summary>
    /// Try to claim the returning entry for <paramref name="accountId"/>.
    /// On success the entry is removed (one-shot).
    /// </summary>
    bool TryConsume(int accountId, int loginId1, byte sex);
}
