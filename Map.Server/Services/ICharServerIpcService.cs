namespace Map.Server.Services;

/// <summary>
/// IPC operations for communicating with the Char server.
/// </summary>
public interface ICharServerIpcService
{
    Task<bool> ValidateCharAuthTicketAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        CancellationToken cancellationToken = default);
}
