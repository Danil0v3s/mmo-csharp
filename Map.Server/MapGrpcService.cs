using Core.Server.IPC;
using Grpc.Core;
using Map.Server.Services;

namespace Map.Server;

public class MapGrpcService : MapService.MapServiceBase
{
    private readonly ICharServerIpcService _charServerIpc;
    private readonly IPlayerMapService _playerMapService;

    public MapGrpcService(
        ICharServerIpcService charServerIpc,
        IPlayerMapService playerMapService)
    {
        _charServerIpc = charServerIpc;
        _playerMapService = playerMapService;
    }

    public override async Task<EnterMapResponse> EnterMap(
        EnterMapRequest request, 
        ServerCallContext context)
    {
        if (request.AccountId > 0 && request.LoginId1 > 0 && request.LoginId2 > 0)
        {
            var authOk = await _charServerIpc.ValidateCharAuthTicketAsync(
                request.AccountId,
                request.CharacterId,
                request.LoginId1,
                request.LoginId2,
                context.CancellationToken);

            if (!authOk)
            {
                return new EnterMapResponse
                {
                    Success = false,
                    ErrorMessage = "Character auth ticket rejected by char server"
                };
            }
        }

        _playerMapService.AddPlayerToMap(
            request.CharacterId,
            (uint)request.MapId,
            request.PositionX,
            request.PositionY,
            request.PositionZ);

        return new EnterMapResponse
        {
            Success = true
        };
    }

    public override Task<LeaveMapResponse> LeaveMap(
        LeaveMapRequest request, 
        ServerCallContext context)
    {
        _playerMapService.RemovePlayerFromMap(request.CharacterId);

        return Task.FromResult(new LeaveMapResponse
        {
            Success = true
        });
    }

    public override Task<MapInfoResponse> GetMapInfo(
        MapInfoRequest request, 
        ServerCallContext context)
    {
        var response = new MapInfoResponse
        {
            MapId = request.MapId,
            MapName = "Test Map"
        };

        foreach (var player in _playerMapService.GetPlayersOnMap((uint)request.MapId))
        {
            response.Players.Add(new PlayerInfo
            {
                CharacterId = player.CharacterId,
                Name = $"Char{player.CharacterId}",
                PositionX = player.PositionX,
                PositionY = player.PositionY,
                PositionZ = player.PositionZ
            });
        }

        return Task.FromResult(response);
    }
}
