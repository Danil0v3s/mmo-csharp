using Char.Server.Services;
using Core.Server.IPC;
using Grpc.Core;

namespace Char.Server;

public class CharGrpcService : CharacterService.CharacterServiceBase
{
    private readonly CharServerImpl _charServer;
    private readonly IMapAuthTicketService _mapAuthTicketService;

    public CharGrpcService(
        CharServerImpl charServer,
        IMapAuthTicketService mapAuthTicketService)
    {
        _charServer = charServer;
        _mapAuthTicketService = mapAuthTicketService;
    }

    public override Task<CharacterListResponse> GetCharacterList(
        CharacterListRequest request, 
        ServerCallContext context)
    {
        // TODO: Query from database
        var response = new CharacterListResponse();
        response.Characters.Add(new CharacterInfo
        {
            CharacterId = 1001,
            Name = "Warrior123",
            Level = 50,
            ClassId = 1,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds()
        });
        response.Characters.Add(new CharacterInfo
        {
            CharacterId = 1002,
            Name = "Mage456",
            Level = 45,
            ClassId = 2,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-20).ToUnixTimeSeconds()
        });

        return Task.FromResult(response);
    }

    public override Task<CreateCharacterResponse> CreateCharacter(
        CreateCharacterRequest request, 
        ServerCallContext context)
    {
        // TODO: Create in database
        var response = new CreateCharacterResponse
        {
            Success = true,
            Character = new CharacterInfo
            {
                CharacterId = new Random().Next(10000, 99999),
                Name = request.Name,
                Level = 1,
                ClassId = request.ClassId,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        return Task.FromResult(response);
    }

    public override Task<DeleteCharacterResponse> DeleteCharacter(
        DeleteCharacterRequest request, 
        ServerCallContext context)
    {
        // TODO: Delete from database
        var response = new DeleteCharacterResponse
        {
            Success = true
        };

        return Task.FromResult(response);
    }

    public override Task<CharacterDataResponse> GetCharacterData(
        CharacterDataRequest request, 
        ServerCallContext context)
    {
        // TODO: Query from database
        var response = new CharacterDataResponse
        {
            Character = new CharacterInfo
            {
                CharacterId = request.CharacterId,
                Name = "TestChar",
                Level = 50,
                ClassId = 1,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds()
            },
            MapId = 1,
            PositionX = 100.0f,
            PositionY = 0.0f,
            PositionZ = 100.0f
        };

        return Task.FromResult(response);
    }

    public override Task<MapAuthTicketResponse> IssueMapAuthTicket(
        MapAuthTicketRequest request,
        ServerCallContext context)
    {
        var success = _mapAuthTicketService.IssueTicket(
            request.AccountId,
            request.CharacterId,
            request.LoginId1,
            request.LoginId2,
            request.Sex,
            request.ClientType,
            request.TtlSeconds);

        return Task.FromResult(new MapAuthTicketResponse
        {
            Success = success,
            ErrorMessage = success ? string.Empty : "Invalid auth ticket request"
        });
    }

    public override Task<MapAuthConsumeResponse> ConsumeMapAuthTicket(
        MapAuthConsumeRequest request,
        ServerCallContext context)
    {
        var success = _mapAuthTicketService.TryConsumeTicket(
            request.AccountId,
            request.CharacterId,
            request.LoginId1,
            request.LoginId2,
            out var sex,
            out var clientType);

        return Task.FromResult(new MapAuthConsumeResponse
        {
            Success = success,
            ErrorMessage = success ? string.Empty : "Map auth ticket missing/expired/mismatch",
            Sex = sex,
            ClientType = clientType
        });
    }

    public override async Task<ForceDisconnectAccountResponse> ForceDisconnectAccount(
        ForceDisconnectAccountRequest request,
        ServerCallContext context)
    {
        var disconnected = await _charServer.ForceDisconnectAccountAsync(request.AccountId);
        return new ForceDisconnectAccountResponse
        {
            Success = true,
            DisconnectedSessions = disconnected
        };
    }

    public override async Task<AccountStatusBroadcastResponse> BroadcastAccountStatusUpdate(
        AccountStatusBroadcastRequest request,
        ServerCallContext context)
    {
        await _charServer.HandleAccountStatusBroadcastAsync(request.AccountId, request.IsBan, request.Value);
        return new AccountStatusBroadcastResponse { Success = true };
    }

    public override async Task<AccountSexBroadcastResponse> BroadcastAccountSexUpdate(
        AccountSexBroadcastRequest request,
        ServerCallContext context)
    {
        await _charServer.HandleAccountSexBroadcastAsync(request.AccountId, request.Sex);
        return new AccountSexBroadcastResponse { Success = true };
    }

    public override Task<AddressSyncResponse> RequestAddressSync(
        AddressSyncRequest request,
        ServerCallContext context)
    {
        _charServer.TriggerAddressSync();
        return Task.FromResult(new AddressSyncResponse { Success = true });
    }

    public override async Task<AccountVipPushResponse> PushVipData(
        AccountVipPushRequest request,
        ServerCallContext context)
    {
        await _charServer.HandleVipDataPushAsync(
            request.AccountId,
            request.VipTime,
            request.Flags,
            request.GroupId,
            request.MapServerId,
            request.IsVip,
            request.CharSlots,
            request.CharVip,
            request.OldGroup);
        return new AccountVipPushResponse { Success = true };
    }
}
