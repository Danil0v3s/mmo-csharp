using Core.Server.IPC;
using Grpc.Core;

namespace Map.Server;

public class MapGrpcService : MapService.MapServiceBase
{
    public override Task<EnterMapResponse> EnterMap(
        EnterMapRequest request, 
        ServerCallContext context)
    {
        // This would be called by CharServer when a player selects a character
        var response = new EnterMapResponse
        {
            Success = true
        };

        return Task.FromResult(response);
    }

    public override Task<LeaveMapResponse> LeaveMap(
        LeaveMapRequest request, 
        ServerCallContext context)
    {
        var response = new LeaveMapResponse
        {
            Success = true
        };

        return Task.FromResult(response);
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

        // TODO: Add actual players in the map
        response.Players.Add(new PlayerInfo
        {
            CharacterId = 1001,
            Name = "Player1",
            PositionX = 100.0f,
            PositionY = 0.0f,
            PositionZ = 100.0f
        });

        return Task.FromResult(response);
    }
}

