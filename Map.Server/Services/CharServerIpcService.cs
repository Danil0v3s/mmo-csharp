using Core.Server;
using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService : ICharServerIpcService
{
    private readonly IServerConnectionService _connectionService;

    public CharServerIpcService(IServerConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    private CharacterService.CharacterServiceClient? GetClient()
    {
        var charSession = _connectionService.GetSessionsByType(ServerType.Char).FirstOrDefault();
        return charSession?.IsConnected == true
            ? new CharacterService.CharacterServiceClient(charSession.Channel)
            : null;
    }
}
