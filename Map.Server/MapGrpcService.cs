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
            request.AccountId,
            (uint)mapId,
            posX,
            posY,
            posZ);

        // P6 post-auth wiring (rAthena chrif post-load equivalents): with the auth ticket
        // accepted, pull state owned by char server and mark online. Each call is best-effort
        // — failures log and proceed, since char-server unreachability is handled by the
        // periodic save/keepalive loop.
        if (request.AccountId > 0 && request.CharacterId > 0)
        {
            try
            {
                await _charServerIpc.SetCharacterOnlineAsync(
                    request.AccountId,
                    request.CharacterId,
                    context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SetCharacterOnline failed for char {CharId}", request.CharacterId);
            }

            try
            {
                _ = await _charServerIpc.LoadSkillCooldownAsync(request.CharacterId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LoadSkillCooldown failed for char {CharId}", request.CharacterId);
            }

            try
            {
                _ = await _charServerIpc.RequestStatusChangeDataAsync(request.CharacterId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RequestStatusChangeData failed for char {CharId}", request.CharacterId);
            }

            try
            {
                _ = await _charServerIpc.GetBonusScriptAsync(request.CharacterId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetBonusScript failed for char {CharId}", request.CharacterId);
            }
        }

        return new EnterMapResponse
        {
            Success = true
        };
    }

    public override async Task<LeaveMapResponse> LeaveMap(
        LeaveMapRequest request,
        ServerCallContext context)
    {
        // P6 disconnect wiring (rAthena chrif logout equivalents): persist final state
        // owned by the map server, then clear the online flag on the char server.
        var player = _playerMapService.RemovePlayerAndGet(request.CharacterId);
        if (player != null && player.AccountId > 0)
        {
            try
            {
                await _charServerIpc.SaveCharacterStateAsync(
                    player.AccountId,
                    player.CharacterId,
                    setOfflineAfterSave: false,
                    finalSave: true,
                    context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Final SaveCharacterState failed for char {CharId}", player.CharacterId);
            }

            try
            {
                await _charServerIpc.SetCharacterOfflineAsync(
                    player.AccountId,
                    player.CharacterId,
                    context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SetCharacterOffline failed for char {CharId}", player.CharacterId);
            }
        }

        return new LeaveMapResponse
        {
            Success = true
        };
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

    // --- P5 inter-base push receivers ---
    // These accept pushes from the char server and log them. Game-client emission
    // (broadcasting to connected players, displaying announce text, etc.) is
    // wired in the map gameplay phase. For now the receivers serve to prove the
    // round-trip works end-to-end and to keep the proto contract enforced.

    public override Task<MapBroadcastAck> ReceiveBroadcast(
        MapBroadcastNotification request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received inter-broadcast from {SourceName} (acc={AccountId}, char={CharId}): {Message}",
            request.SourceName, request.SourceAccountId, request.SourceCharacterId, request.Message);
        return Task.FromResult(new MapBroadcastAck { Success = true });
    }

    public override Task<MapBroadcastAck> ReceiveItemBroadcast(
        MapItemBroadcastNotification request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received inter-item-broadcast from {SourceName}: item={ItemId} amount={Amount}",
            request.SourceName, request.ItemId, request.Amount);
        return Task.FromResult(new MapBroadcastAck { Success = true });
    }

    public override Task<MapWhisperAck> ReceiveWhisper(
        MapWhisperNotification request,
        ServerCallContext context)
    {
        var delivered = _playerMapService.IsPlayerOnAnyMap(request.TargetCharacterId);
        _logger.LogInformation(
            "Received whisper for {TargetName} (char={CharId}) from {Source}; delivered={Delivered}",
            request.TargetName, request.TargetCharacterId, request.SourceName, delivered);
        return Task.FromResult(new MapWhisperAck { Success = true, Delivered = delivered });
    }

    public override Task<MapBroadcastAck> ReceiveWhisperToGm(
        MapWhisperToGmNotification request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received whisper-to-gm from {Source} (min group {MinGroup}): {Message}",
            request.SourceName, request.MinGroupId, request.Message);
        return Task.FromResult(new MapBroadcastAck { Success = true });
    }

    public override Task<MapBroadcastAck> NotifyNameChange(
        MapNameChangeNotification request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received name-change for entity type={Type} id={Id} new={NewName}",
            request.EntityType, request.EntityId, request.NewName);
        return Task.FromResult(new MapBroadcastAck { Success = true });
    }

    public override Task<MapBroadcastAck> NotifyAddressSync(
        MapAddressSyncNotification request,
        ServerCallContext context)
    {
        _logger.LogInformation("Received address-sync notification from char server");
        return Task.FromResult(new MapBroadcastAck { Success = true });
    }

    public override Task<MapForceDisconnectAck> ForceDisconnectAccount(
        MapForceDisconnectNotification request,
        ServerCallContext context)
    {
        var victims = _playerMapService.GetAllPlayers()
            .Where(p => p.AccountId == request.AccountId)
            .Select(p => p.CharacterId)
            .ToList();

        foreach (var charId in victims)
        {
            _playerMapService.RemovePlayerFromMap(charId);
        }

        _logger.LogInformation(
            "ForceDisconnectAccount: removed {Count} sessions for account {AccountId}",
            victims.Count, request.AccountId);

        return Task.FromResult(new MapForceDisconnectAck
        {
            Success = true,
            DisconnectedCount = victims.Count
        });
    }
}
