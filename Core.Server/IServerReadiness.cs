namespace Core.Server;

/// <summary>
/// "Is this server fully booted yet?" Each server defines its own
/// readiness rule:
///   - Login: TCP listener up + server state Running.
///   - Char:  + registered with the Login server.
///   - Map:   + map list registered with the Char server.
///
/// Exposed over the wire by the internal ping handler (CZ_INTERNAL_PING)
/// so the test harness can poll until the stack is safe to drive instead
/// of scraping log files.
/// </summary>
public interface IServerReadiness
{
    bool IsReady { get; }
}
