using Core.Server.IPC;
using Grpc.Core;
using Map.Server.Services;

namespace Map.Server;

public class MapGrpcService : MapService.MapServiceBase
{
    private readonly ICharServerIpcService _charServerIpc;
    private readonly IPlayerMapService _playerMapService;
    private readonly ILogger<MapGrpcService> _logger;

    public MapGrpcService(
        ICharServerIpcService charServerIpc,
        IPlayerMapService playerMapService,
        ILogger<MapGrpcService> logger)
    {
        _charServerIpc = charServerIpc;
        _playerMapService = playerMapService;
        _logger = logger;
    }

    public override async Task<EnterMapResponse> EnterMap(
        EnterMapRequest request, 
        ServerCallContext context)
    {
        var mapId = request.MapId;
        var posX = request.PositionX;
        var posY = request.PositionY;
        var posZ = request.PositionZ;

        if (request.AccountId > 0 && request.LoginId1 > 0 && request.LoginId2 > 0)
        {
            var auth = await _charServerIpc.RequestCharacterMapAuthAsync(
                request.AccountId,
                request.CharacterId,
                request.LoginId1,
                request.LoginId2,
                request.Sex,
                0,
                autotrade: false,
                context.CancellationToken);

            if (auth?.Success != true)
            {
                return new EnterMapResponse
                {
                    Success = false,
                    ErrorMessage = auth?.ErrorMessage ?? "Character auth ticket rejected by char server"
                };
            }

            _logger.LogInformation(
                "Map auth accepted for account {AccountId}, char {CharacterId} (sex={Sex}, clientType={ClientType})",
                request.AccountId,
                request.CharacterId,
                request.Sex,
                auth.CharacterData?.Character?.ClassId ?? 0);

            if (mapId <= 0 && auth.CharacterData != null)
            {
                mapId = auth.CharacterData.MapId;
                posX = auth.CharacterData.PositionX;
                posY = auth.CharacterData.PositionY;
                posZ = auth.CharacterData.PositionZ;
            }
        }

        // If caller did not provide a spawn/map, pull authoritative data from Char server.
        if (mapId <= 0)
        {
            var characterData = await _charServerIpc.GetCharacterDataAsync(
                request.CharacterId,
                context.CancellationToken);

            if (characterData?.Character == null || characterData.MapId <= 0)
            {
                return new EnterMapResponse
                {
                    Success = false,
                    ErrorMessage = "Character data unavailable from char server"
                };
            }

            mapId = characterData.MapId;
            posX = characterData.PositionX;
            posY = characterData.PositionY;
            posZ = characterData.PositionZ;
        }

        _playerMapService.AddPlayerToMap(
            request.CharacterId,
            (uint)mapId,
            posX,
            posY,
            posZ);

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
