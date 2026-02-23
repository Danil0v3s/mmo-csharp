using Core.Server;
using Core.Server.IPC;

namespace Login.Server.UseCase;

public class CharServerIpcService(
    IServerConnectionService connectionService,
    ILogger<CharServerIpcService> logger
) : ICharServerIpcService
{
    public async Task ForceDisconnectAccountFromCharServersAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var charSessions = connectionService.GetSessionsByType(ServerType.Char).ToList();
        if (charSessions.Count == 0)
        {
            return;
        }

        foreach (var charSession in charSessions)
        {
            try
            {
                var client = new CharacterService.CharacterServiceClient(charSession.Channel);
                var response = await client.ForceDisconnectAccountAsync(
                    new ForceDisconnectAccountRequest { AccountId = accountId },
                    cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Duplicate-session force disconnect requested for account {AccountId} on char server {ServerName} (disconnected: {Disconnected})",
                    accountId,
                    charSession.ServerName,
                    response.DisconnectedSessions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to request duplicate-session disconnect for account {AccountId} on char server {ServerName}",
                    accountId,
                    charSession.ServerName);
            }
        }
    }

    public async Task RequestCharServerAddressSyncAsync(CancellationToken cancellationToken = default)
    {
        var charSessions = connectionService.GetSessionsByType(ServerType.Char).ToList();
        foreach (var charSession in charSessions)
        {
            try
            {
                var client = new CharacterService.CharacterServiceClient(charSession.Channel);
                await client.RequestAddressSyncAsync(new AddressSyncRequest(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to request char address sync for server {ServerName}",
                    charSession.ServerName);
            }
        }
    }
}
