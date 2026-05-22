namespace Map.Server.Services;

/// <summary>
/// Char-server mapreg IPC — persistence for global script variables
/// (rAthena <c>$varname</c> + <c>@varname</c>). Mirrors rAthena
/// <c>chrif_mapreg_recv</c> / <c>chrif_mapreg_send</c> in
/// <c>chrif.cpp</c>. The actual gRPC binding lands when the
/// script-engine consumer ports its <c>$var</c> read/write
/// builtins (Phase 4 of <c>map/scripting/README.md</c>) — until
/// then the canonical seam exists so the rAthena
/// <c>intif_request_mapreg</c> / <c>intif_save_mapreg</c> entry
/// points on <see cref="Intif.IIntifService"/> can route through
/// a typed wrapper instead of returning 0.
/// </summary>
public interface ICharServerIpcServiceMapreg
{
    /// <summary>
    /// Pull the persisted global-script-variable table from the char
    /// server. <c>true</c> on dispatch success.
    /// </summary>
    Task<bool> RequestMapregAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Push the in-memory mapreg snapshot up to the char server for
    /// persistence. <paramref name="data"/> is the opaque byte
    /// payload (same shape as account-storage / guild-storage —
    /// the script engine owns the encode/decode).
    /// </summary>
    Task<bool> SaveMapregAsync(byte[] data, CancellationToken cancellationToken = default);
}
