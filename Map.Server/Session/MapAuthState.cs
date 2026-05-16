namespace Map.Server.Session;

/// <summary>
/// Auth lifecycle of a TCP-connected map client. Progresses linearly:
///   <c>Unauthenticated</c> — TCP up, no <c>CZ_WANT_TO_CONNECTION</c> yet.
///   <c>Authenticated</c> — ticket validated against char server, ZC_AID
///        and ZC_ACCEPT_ENTER_ZONE sent. No <see cref="PlayerEntity"/> yet.
///   <c>Spawned</c> — client sent <c>CZ_NOTIFY_ACTORINIT</c>; the
///        <see cref="PlayerEntity"/> exists and is broadcasting to peers.
/// </summary>
public enum MapAuthState
{
    Unauthenticated = 0,
    Authenticated = 1,
    Spawned = 2,
}
