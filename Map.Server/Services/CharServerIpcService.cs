using Core.Server;
using Core.Server.IPC;

namespace Map.Server.Services;

public class CharServerIpcService(
    IServerConnectionService connectionService
) : ICharServerIpcService
{
    public async Task<bool> ValidateCharAuthTicketAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        CancellationToken cancellationToken = default)
    {
        var charSession = connectionService.GetSessionsByType(ServerType.Char).FirstOrDefault();
        if (charSession?.IsConnected != true)
        {
            return false;
        }

        var charClient = new CharacterService.CharacterServiceClient(charSession.Channel);
        var response = await charClient.ConsumeMapAuthTicketAsync(new MapAuthConsumeRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            LoginId1 = loginId1,
            LoginId2 = loginId2
        }, cancellationToken: cancellationToken);

        return response.Success;
    }
}
