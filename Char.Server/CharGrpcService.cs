using Char.Server.Services;
using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server;
using Grpc.Core;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Char.Server;

public class CharGrpcService : CharacterService.CharacterServiceBase
{
    private const int MaxCharacterNameLength = 24;
    private const int FameTypeBlacksmith = 0;
    private const int FameTypeAlchemist = 1;
    private const int FameTypeTaekwon = 2;
    private static readonly ushort[] BlacksmithFameClasses =
    [
        10,   // JOB_BLACKSMITH
        4011, // JOB_WHITESMITH
        4033, // JOB_BABY_BLACKSMITH
        4058, // JOB_MECHANIC
        4064, // JOB_MECHANIC_T
        4100, // JOB_BABY_MECHANIC
        4253  // JOB_MEISTER
    ];
    private static readonly ushort[] AlchemistFameClasses =
    [
        18,   // JOB_ALCHEMIST
        4019, // JOB_CREATOR
        4041, // JOB_BABY_ALCHEMIST
        4071, // JOB_GENETIC
        4078, // JOB_GENETIC_T
        4107, // JOB_BABY_GENETIC
        4259  // JOB_BIOLO
    ];
    private static readonly ushort[] TaekwonFameClasses =
    [
        4046, // JOB_TAEKWON
        4225  // JOB_BABY_TAEKWON
    ];
    private readonly CharServerImpl _charServer;
    private readonly IMapAuthTicketService _mapAuthTicketService;
    private readonly IReturningClientAuthService _returningClientAuth;
    private readonly IMapServerRegistryService _mapServerRegistry;
    private readonly ILoginServerIpcService _loginServerIpc;
    private readonly IMapServerIpcService _mapServerIpc;
    private readonly ICharacterRepository _characterRepository;
    private readonly IFriendRepository _friendRepository;
    private readonly GameDbContext _dbContext;
    private readonly CharServerConfiguration _configuration;
    private readonly ILogger<CharGrpcService> _logger;

    public CharGrpcService(
        CharServerImpl charServer,
        IMapAuthTicketService mapAuthTicketService,
        IReturningClientAuthService returningClientAuth,
        IMapServerRegistryService mapServerRegistry,
        ILoginServerIpcService loginServerIpc,
        IMapServerIpcService mapServerIpc,
        ICharacterRepository characterRepository,
        IFriendRepository friendRepository,
        GameDbContext dbContext,
        CharServerConfiguration configuration,
        ILogger<CharGrpcService> logger)
    {
        _charServer = charServer;
        _mapAuthTicketService = mapAuthTicketService;
        _returningClientAuth = returningClientAuth;
        _mapServerRegistry = mapServerRegistry;
        _loginServerIpc = loginServerIpc;
        _mapServerIpc = mapServerIpc;
        _characterRepository = characterRepository;
        _friendRepository = friendRepository;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task<CharacterListResponse> GetCharacterList(
        CharacterListRequest request, 
        ServerCallContext context)
    {
        var response = new CharacterListResponse();

        if (request.AccountId <= 0)
        {
            return response;
        }

        var characters = await _characterRepository.GetByAccountIdAsync((int)request.AccountId, context.CancellationToken);
        foreach (var character in characters
                     .Where(c => c.DeleteDate == 0)
                     .OrderBy(c => c.CharNum))
        {
            response.Characters.Add(ToCharacterInfo(character));
        }

        return response;
    }

    public override async Task<CreateCharacterResponse> CreateCharacter(
        CreateCharacterRequest request, 
        ServerCallContext context)
    {
        if (!_configuration.CharNew)
        {
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "Character creation is disabled"
            };
        }

        if (request.AccountId <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "Invalid character create request"
            };
        }

        var normalizedName = request.Name.Trim();
        if (normalizedName.Length < _configuration.Char.CharNameMinLength || normalizedName.Length > MaxCharacterNameLength)
        {
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "Invalid character name length"
            };
        }

        if (await _characterRepository.NameExistsAsync(normalizedName, context.CancellationToken))
        {
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "Character name already exists"
            };
        }

        var characters = await _characterRepository.GetByAccountIdAsync((int)request.AccountId, context.CancellationToken);
        var activeCharacters = characters.Where(c => c.DeleteDate == 0).ToList();

        if (_configuration.Char.CharPerAccount > 0 && activeCharacters.Count >= _configuration.Char.CharPerAccount)
        {
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "Character slots are full"
            };
        }

        if (!TryFindAvailableSlot(activeCharacters, out var slot))
        {
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "No available character slot"
            };
        }

        var startPoint = _configuration.StartPoint.FirstOrDefault() ?? new StartPoint { Map = "prontera", X = 156, Y = 191 };
        var nowUtc = DateTime.UtcNow;
        var entity = new CharEntity
        {
            AccountId = (int)request.AccountId,
            CharNum = slot,
            Name = normalizedName,
            Class = (ushort)Math.Max(request.ClassId, 0),
            BaseLevel = 1,
            JobLevel = 1,
            LastMap = startPoint.Map,
            LastX = (ushort)startPoint.X,
            LastY = (ushort)startPoint.Y,
            SaveMap = startPoint.Map,
            SaveX = (ushort)startPoint.X,
            SaveY = (ushort)startPoint.Y,
            Online = 0,
            DeleteDate = 0,
            LastLogin = nowUtc
        };

        try
        {
            entity = await _characterRepository.AddAsync(entity, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed creating character for account {AccountId}", request.AccountId);
            return new CreateCharacterResponse
            {
                Success = false,
                ErrorMessage = "Database error"
            };
        }

        return new CreateCharacterResponse
        {
            Success = true,
            Character = ToCharacterInfo(entity)
        };
    }

    public override async Task<DeleteCharacterResponse> DeleteCharacter(
        DeleteCharacterRequest request, 
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return new DeleteCharacterResponse { Success = false };
        }

        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is null || character.AccountId != request.AccountId || character.DeleteDate != 0)
        {
            return new DeleteCharacterResponse { Success = false };
        }

        if (IsDeleteBlockedByBaseLevel(character.BaseLevel, _configuration.Char.CharDeleteLevel))
        {
            return new DeleteCharacterResponse { Success = false };
        }

        if ((_configuration.Char.CharDeleteRestriction & 0x02) != 0 && character.GuildId != 0)
        {
            return new DeleteCharacterResponse { Success = false };
        }

        if ((_configuration.Char.CharDeleteRestriction & 0x01) != 0 && character.PartyId != 0)
        {
            return new DeleteCharacterResponse { Success = false };
        }

        // rAthena-compatible soft-delete visibility behavior for char-list:
        // set delete timestamp so it disappears from active list without hard row removal.
        character.DeleteDate = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        character.Online = 0;

        try
        {
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed deleting character {CharacterId} for account {AccountId}",
                request.CharacterId,
                request.AccountId);
            return new DeleteCharacterResponse { Success = false };
        }

        return new DeleteCharacterResponse { Success = true };
    }

    public override async Task<CharacterDataResponse> GetCharacterData(
        CharacterDataRequest request, 
        ServerCallContext context)
    {
        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        return character is null ? new CharacterDataResponse() : ToCharacterDataResponse(character);
    }

    public override Task<MapServerChangeResponse> RequestMapServerChange(
        MapServerChangeRequest request,
        ServerCallContext context)
    {
        // Equivalent to rAthena's 0x2b05 ack/nak behavior:
        // verify server state, target map server connectivity, then issue transfer auth ticket.
        if (_charServer.State != ServerState.Running ||
            request.AccountId <= 0 ||
            request.CharacterId <= 0 ||
            request.LoginId1 <= 0 ||
            request.LoginId2 <= 0 ||
            string.IsNullOrWhiteSpace(request.MapName))
        {
            return Task.FromResult(new MapServerChangeResponse
            {
                Success = false,
                ErrorMessage = "Invalid map-server change request"
            });
        }

        if (!_charServer.ServerConnections.HasConnection(Core.Server.IPC.ServerType.Map))
        {
            return Task.FromResult(new MapServerChangeResponse
            {
                Success = false,
                ErrorMessage = "No active map server connection"
            });
        }

        if (request.TargetMapServerId > 0 && !_mapServerRegistry.HasServer(request.TargetMapServerId))
        {
            return Task.FromResult(new MapServerChangeResponse
            {
                Success = false,
                ErrorMessage = "Unknown target map server"
            });
        }

        if (request.TargetMapServerId <= 0 && !_mapServerRegistry.ContainsMap(request.MapName))
        {
            return Task.FromResult(new MapServerChangeResponse
            {
                Success = false,
                ErrorMessage = "No registered map server for requested map"
            });
        }

        var issued = _mapAuthTicketService.IssueTicket(
            request.AccountId,
            request.CharacterId,
            request.LoginId1,
            request.LoginId2,
            request.Sex,
            request.ClientType,
            request.TtlSeconds);

        return Task.FromResult(new MapServerChangeResponse
        {
            Success = issued,
            ErrorMessage = issued ? string.Empty : "Failed to issue map auth ticket"
        });
    }

    public override async Task<CharacterMapAuthResponse> RequestCharacterMapAuth(
        CharacterMapAuthRequest request,
        ServerCallContext context)
    {
        if (_charServer.State != ServerState.Running || request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return new CharacterMapAuthResponse
            {
                Success = false,
                ErrorMessage = "Invalid map auth request"
            };
        }

        // Autotrade bypass parity path.
        if (request.Autotrade)
        {
            var autotradeCharacter = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
            if (autotradeCharacter is null || autotradeCharacter.AccountId != request.AccountId)
            {
                return new CharacterMapAuthResponse
                {
                    Success = false,
                    ErrorMessage = "Character not found",
                    AccountId = request.AccountId,
                    CharacterId = request.CharacterId
                };
            }

            return new CharacterMapAuthResponse
            {
                Success = true,
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                LoginId1 = 0,
                LoginId2 = 0,
                ExpirationTime = 0,
                GroupId = 0,
                ChangingMapServers = false,
                CharacterData = ToCharacterDataResponse(autotradeCharacter),
                Sex = SexCharToByte(autotradeCharacter.Sex)
            };
        }

        if (!_mapAuthTicketService.TryConsumeTicket(
                request.AccountId,
                request.CharacterId,
                request.LoginId1,
                request.LoginId2,
                out var sex,
                out var clientType))
        {
            return new CharacterMapAuthResponse
            {
                Success = false,
                ErrorMessage = "Auth ticket missing/expired/mismatch",
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                LoginId1 = request.LoginId1,
                LoginId2 = request.LoginId2
            };
        }

        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is null || character.AccountId != request.AccountId)
        {
            return new CharacterMapAuthResponse
            {
                Success = false,
                ErrorMessage = "Character not found",
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                LoginId1 = request.LoginId1,
                LoginId2 = request.LoginId2
            };
        }

        var characterData = ToCharacterDataResponse(character);

        // Fetch account group id from login server so the map server can gate
        // GM commands ([adjacent/chat.md]'s @killmob/@warp). Best-effort —
        // unreachable login server falls back to 0 (no GM access).
        uint groupId = 0;
        try
        {
            var accountInfo = await _loginServerIpc.RequestDetailedAccountInfoAsync(
                request.AccountId, context.CancellationToken);
            if (accountInfo?.Success == true && accountInfo.GroupId > 0)
            {
                groupId = (uint)accountInfo.GroupId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Failed to fetch account info for {AccountId}; GroupId defaults to 0",
                request.AccountId);
        }

        return new CharacterMapAuthResponse
        {
            Success = true,
            AccountId = request.AccountId,
            CharacterId = request.CharacterId,
            LoginId1 = request.LoginId1,
            LoginId2 = request.LoginId2,
            ExpirationTime = 0,
            GroupId = groupId,
            ChangingMapServers = true,
            CharacterData = characterData,
            Sex = sex
        };
    }

    public override Task<SaveCharacterStateResponse> SaveCharacterState(
        SaveCharacterStateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return Task.FromResult(new SaveCharacterStateResponse
            {
                Success = false,
                ErrorMessage = "Invalid save character request",
                SaveAck = false
            });
        }

        return SaveCharacterStateInternalAsync(request, context);
    }

    private async Task<SaveCharacterStateResponse> SaveCharacterStateInternalAsync(
        SaveCharacterStateRequest request,
        ServerCallContext context)
    {
        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is null || character.AccountId != request.AccountId)
        {
            return new SaveCharacterStateResponse
            {
                Success = false,
                ErrorMessage = "Character not found",
                SaveAck = false
            };
        }

        // rAthena parity: save-char always persists; when "set offline" flag is present,
        // mark character offline after save and emit save-ack.
        if (request.SetOfflineAfterSave)
        {
            character.Online = 0;
        }

        character.LastLogin = DateTime.UtcNow;

        try
        {
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist SaveCharacterState for account {AccountId}, character {CharacterId}",
                request.AccountId,
                request.CharacterId);
            return new SaveCharacterStateResponse
            {
                Success = false,
                ErrorMessage = "Database error",
                SaveAck = false
            };
        }

        if (request.SetOfflineAfterSave && _charServer.RegisteredServerId >= 0)
        {
            await _loginServerIpc.NotifyAccountStatusAsync(
                request.AccountId,
                _charServer.RegisteredServerId,
                online: false,
                context.CancellationToken);
        }

        return new SaveCharacterStateResponse
        {
            Success = true,
            SaveAck = request.SetOfflineAfterSave
        };
    }

    public override Task<CharacterSelectAuthOkResponse> NotifyCharacterSelectAuthOk(
        CharacterSelectAuthOkRequest request,
        ServerCallContext context)
    {
        if (_charServer.State != ServerState.Running || request.AccountId <= 0)
        {
            return Task.FromResult(new CharacterSelectAuthOkResponse
            {
                Success = false,
                ErrorMessage = "Char server not ready"
            });
        }

        // rAthena chmapif_parse_reqcharselect parity: stash an auth_db entry
        // so the imminent CH_REQ_TO_CONNECT from the returning client is
        // accepted without re-auth against the login server (whose auth_node
        // for this account was consumed by the initial char→map handoff).
        _returningClientAuth.Allow(
            accountId: request.AccountId,
            loginId1: request.LoginId1,
            sex: (byte)request.Sex);

        return Task.FromResult(new CharacterSelectAuthOkResponse
        {
            Success = true
        });
    }

    public override Task<CharacterKeepAliveResponse> KeepAlive(
        CharacterKeepAliveRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new CharacterKeepAliveResponse { Success = true });
    }

    public override Task<MapServerMapRegistryResponse> RegisterMapServerMaps(
        MapServerMapRegistryRequest request,
        ServerCallContext context)
    {
        if (request.MapServerId <= 0)
        {
            return Task.FromResult(new MapServerMapRegistryResponse { Success = false, RegisteredMaps = 0 });
        }

        var registeredMaps = _mapServerRegistry.RegisterMaps(request.MapServerId, request.MapNames);

        return Task.FromResult(new MapServerMapRegistryResponse
        {
            Success = true,
            RegisteredMaps = registeredMaps
        });
    }

    public override Task<MapServerUserCountResponse> GetMapServerUserCount(
        MapServerUserCountRequest request,
        ServerCallContext context)
    {
        if (request.MapServerId <= 0)
        {
            return Task.FromResult(new MapServerUserCountResponse { Success = false, UserCount = 0 });
        }

        var exists = _mapServerRegistry.TryGetUserCount(request.MapServerId, out var users);
        return Task.FromResult(new MapServerUserCountResponse { Success = exists, UserCount = users });
    }

    public override Task<MapServerUserCountUpdateResponse> RegisterMapServerUserCount(
        MapServerUserCountUpdateRequest request,
        ServerCallContext context)
    {
        if (request.MapServerId <= 0 || request.UserCount < 0)
        {
            return Task.FromResult(new MapServerUserCountUpdateResponse { Success = false });
        }

        _mapServerRegistry.SetUserCount(request.MapServerId, request.UserCount);
        return Task.FromResult(new MapServerUserCountUpdateResponse { Success = true });
    }

    public override Task<MapServerAddressUpdateResponse> UpdateMapServerAddress(
        MapServerAddressUpdateRequest request,
        ServerCallContext context)
    {
        if (request.MapServerId <= 0)
        {
            return Task.FromResult(new MapServerAddressUpdateResponse { Success = false });
        }

        _mapServerRegistry.SetAddress(request.MapServerId, request.Ip, request.Port);
        return Task.FromResult(new MapServerAddressUpdateResponse { Success = true });
    }

    public override async Task<StatusChangeDataResponse> RequestStatusChangeData(
        StatusChangeDataRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new StatusChangeDataResponse { Success = false };
        }

        var rows = await _dbContext.StatusChanges
            .AsNoTracking()
            .Where(e => e.CharId == (int)request.CharacterId)
            .OrderBy(e => e.Type)
            .ToListAsync(context.CancellationToken);

        return new StatusChangeDataResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(SerializeStatusChangeRows(rows))
        };
    }

    public override async Task<StatusChangeDataSaveResponse> SaveStatusChangeData(
        StatusChangeDataSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new StatusChangeDataSaveResponse { Success = false };
        }

        if (!TryDeserializeStatusChangeRows(request.Data.ToByteArray(), out var rows))
        {
            return new StatusChangeDataSaveResponse { Success = false };
        }

        var charId = (int)request.CharacterId;
        var existing = await _dbContext.StatusChanges
            .Where(e => e.CharId == charId)
            .ToListAsync(context.CancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.StatusChanges.RemoveRange(existing);
        }

        foreach (var row in rows)
        {
            row.CharId = charId;
            _dbContext.StatusChanges.Add(row);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new StatusChangeDataSaveResponse { Success = true };
    }

    public override async Task<SkillCooldownLoadResponse> LoadSkillCooldown(
        SkillCooldownLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new SkillCooldownLoadResponse { Success = false };
        }

        var rows = await _dbContext.SkillCooldowns
            .AsNoTracking()
            .Where(e => e.CharId == (int)request.CharacterId)
            .OrderBy(e => e.Skill)
            .ToListAsync(context.CancellationToken);

        return new SkillCooldownLoadResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(SerializeSkillCooldownRows(rows))
        };
    }

    public override async Task<SkillCooldownSaveResponse> SaveSkillCooldown(
        SkillCooldownSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new SkillCooldownSaveResponse { Success = false };
        }

        if (!TryDeserializeSkillCooldownRows(request.Data.ToByteArray(), out var rows))
        {
            return new SkillCooldownSaveResponse { Success = false };
        }

        var charId = (int)request.CharacterId;
        var existing = await _dbContext.SkillCooldowns
            .Where(e => e.CharId == charId)
            .ToListAsync(context.CancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.SkillCooldowns.RemoveRange(existing);
        }

        foreach (var row in rows)
        {
            row.CharId = charId;
            _dbContext.SkillCooldowns.Add(row);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new SkillCooldownSaveResponse { Success = true };
    }

    public override async Task<CharacterOnlineStateResponse> SetCharacterOffline(
        CharacterOnlineStateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return new CharacterOnlineStateResponse { Success = false };
        }

        var characters = await _characterRepository.GetByAccountIdAsync(request.AccountId, context.CancellationToken);
        foreach (var character in characters.Where(c => c.DeleteDate == 0 &&
                                                        (request.CharacterId <= 0 || c.CharId == request.CharacterId)))
        {
            if (character.Online == 0)
            {
                continue;
            }

            character.Online = 0;
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
        }

        await _charServer.ForceDisconnectAccountAsync(request.AccountId);

        if (_charServer.RegisteredServerId >= 0)
        {
            await _loginServerIpc.NotifyAccountStatusAsync(
                request.AccountId,
                _charServer.RegisteredServerId,
                online: false,
                context.CancellationToken);
        }

        return new CharacterOnlineStateResponse { Success = true };
    }

    public override async Task<CharacterOnlineStateResponse> SetCharacterOnline(
        CharacterOnlineStateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return new CharacterOnlineStateResponse { Success = false };
        }

        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is null || character.AccountId != request.AccountId || character.DeleteDate != 0)
        {
            return new CharacterOnlineStateResponse { Success = false };
        }

        if (character.Online == 0)
        {
            character.Online = 1;
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
        }

        if (_charServer.RegisteredServerId >= 0)
        {
            await _loginServerIpc.NotifyAccountStatusAsync(
                request.AccountId,
                _charServer.RegisteredServerId,
                online: true,
                context.CancellationToken);
        }

        return new CharacterOnlineStateResponse { Success = true };
    }

    public override async Task<SetAllCharactersOfflineResponse> SetAllCharactersOffline(
        SetAllCharactersOfflineRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Received map-server all-offline request from map server {MapServerId}", request.MapServerId);

        var onlineCharacters = await _characterRepository.GetOnlineCharactersAsync(context.CancellationToken);
        foreach (var character in onlineCharacters)
        {
            character.Online = 0;
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
        }

        if (_charServer.RegisteredServerId >= 0)
        {
            await _loginServerIpc.SetAllOfflineAsync(_charServer.RegisteredServerId, context.CancellationToken);
        }

        return new SetAllCharactersOfflineResponse { Success = true };
    }

    public override async Task<RemoveFriendResponse> RequestRemoveFriend(
        RemoveFriendRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0 || request.FriendCharacterId <= 0)
        {
            return new RemoveFriendResponse { Success = false };
        }

        try
        {
            await _friendRepository.DeleteAsync((int)request.CharacterId, (int)request.FriendCharacterId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed removing friend relation char {CharacterId} -> friend {FriendCharacterId}",
                request.CharacterId,
                request.FriendCharacterId);
            return new RemoveFriendResponse { Success = false };
        }

        return new RemoveFriendResponse { Success = true };
    }

    public override async Task<CharacterNameResponse> RequestCharacterName(
        CharacterNameRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new CharacterNameResponse { Success = false, Name = string.Empty };
        }

        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is null)
        {
            return new CharacterNameResponse { Success = false, Name = string.Empty };
        }

        return new CharacterNameResponse
        {
            Success = true,
            Name = character.Name
        };
    }

    public override async Task<CharacterEmailChangeResponse> RequestEmailChange(
        CharacterEmailChangeRequest request,
        ServerCallContext context)
    {
        var response = await _loginServerIpc.ChangeAccountEmailAsync(
            request.AccountId,
            request.CurrentEmail,
            request.NewEmail,
            context.CancellationToken);

        return new CharacterEmailChangeResponse
        {
            Success = response?.Success == true,
            ErrorMessage = response?.ErrorMessage ?? (response == null ? "Login server unavailable" : string.Empty)
        };
    }

    public override async Task<ForwardAccountStatusChangeResponse> ForwardAccountStatusChange(
        ForwardAccountStatusChangeRequest request,
        ServerCallContext context)
    {
        var response = await _loginServerIpc.UpdateAccountStateAsync(
            request.AccountId,
            request.State,
            context.CancellationToken);

        return new ForwardAccountStatusChangeResponse
        {
            Success = response?.Success == true,
            ErrorMessage = response?.ErrorMessage ?? (response == null ? "Login server unavailable" : string.Empty)
        };
    }

    public override async Task<DivorceResponse> RequestDivorce(
        DivorceRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0 || request.PartnerCharacterId <= 0)
        {
            return new DivorceResponse { Success = false };
        }

        var updated = false;

        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is not null && character.PartnerId != 0)
        {
            character.PartnerId = 0;
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
            updated = true;
        }

        var partner = await LoadCharacterEntityAsync(request.PartnerCharacterId, context.CancellationToken);
        if (partner is not null && partner.PartnerId != 0)
        {
            partner.PartnerId = 0;
            await _characterRepository.UpdateAsync(partner, context.CancellationToken);
            updated = true;
        }

        return new DivorceResponse
        {
            Success = updated
        };
    }

    public override async Task<CharacterBanResponse> RequestCharacterBan(
        CharacterBanRequest request,
        ServerCallContext context)
    {
        var response = await _loginServerIpc.BanAccountAsync(
            request.AccountId,
            request.DurationSeconds,
            context.CancellationToken);

        return new CharacterBanResponse
        {
            Success = response?.Success == true,
            ErrorMessage = response?.ErrorMessage ?? (response == null ? "Login server unavailable" : string.Empty)
        };
    }

    public override async Task<CharacterUnbanResponse> RequestCharacterUnban(
        CharacterUnbanRequest request,
        ServerCallContext context)
    {
        var response = await _loginServerIpc.UnbanAccountAsync(
            request.AccountId,
            context.CancellationToken);

        return new CharacterUnbanResponse
        {
            Success = response?.Success == true,
            ErrorMessage = response?.ErrorMessage ?? (response == null ? "Login server unavailable" : string.Empty)
        };
    }

    public override Task<FameUpdateResponse> UpdateFame(
        FameUpdateRequest request,
        ServerCallContext context)
    {
        return UpdateFameInternalAsync(request, context);
    }

    private async Task<FameUpdateResponse> UpdateFameInternalAsync(
        FameUpdateRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new FameUpdateResponse { Success = false };
        }

        var character = await LoadCharacterEntityAsync(request.CharacterId, context.CancellationToken);
        if (character is null)
        {
            return new FameUpdateResponse { Success = false };
        }

        character.Fame = (uint)Math.Max(request.Value, 0);
        await _characterRepository.UpdateAsync(character, context.CancellationToken);

        return new FameUpdateResponse { Success = true };
    }

    public override async Task<FameListResponse> RequestFameList(
        FameListRequest request,
        ServerCallContext context)
    {
        var response = new FameListResponse { Success = true };
        var allowedClasses = GetFameClassFilter(request.FameType);
        if (allowedClasses is null)
        {
            return response;
        }

        var rankedCharacters = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Fame > 0 && c.DeleteDate == 0 && allowedClasses.Contains(c.Class))
            .OrderByDescending(c => c.Fame)
            .ThenBy(c => c.CharId)
            .Take(10)
            .ToListAsync(context.CancellationToken);

        foreach (var character in rankedCharacters)
        {
            response.Entries.Add(new FameEntry
            {
                CharacterId = character.CharId,
                Value = (int)Math.Min(character.Fame, int.MaxValue)
            });
        }

        return response;
    }

    private static ushort[]? GetFameClassFilter(int fameType)
    {
        return fameType switch
        {
            FameTypeBlacksmith => BlacksmithFameClasses,
            FameTypeAlchemist => AlchemistFameClasses,
            FameTypeTaekwon => TaekwonFameClasses,
            _ => null
        };
    }

    public override async Task<BonusScriptGetResponse> GetBonusScript(
        BonusScriptGetRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new BonusScriptGetResponse { Success = false };
        }

        var rows = await _dbContext.BonusScripts
            .AsNoTracking()
            .Where(e => e.CharId == (int)request.CharacterId)
            .ToListAsync(context.CancellationToken);

        return new BonusScriptGetResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(SerializeBonusScriptRows(rows))
        };
    }

    public override async Task<BonusScriptSaveResponse> SaveBonusScript(
        BonusScriptSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new BonusScriptSaveResponse { Success = false };
        }

        if (!TryDeserializeBonusScriptRows(request.Data.ToByteArray(), out var rows))
        {
            return new BonusScriptSaveResponse { Success = false };
        }

        var charId = (int)request.CharacterId;
        var existing = await _dbContext.BonusScripts
            .Where(e => e.CharId == charId)
            .ToListAsync(context.CancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.BonusScripts.RemoveRange(existing);
        }

        foreach (var row in rows)
        {
            row.CharId = charId;
            _dbContext.BonusScripts.Add(row);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new BonusScriptSaveResponse { Success = true };
    }

    public override async Task<InterBroadcastResponse> InterBroadcast(
        InterBroadcastRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new InterBroadcastResponse { Success = false };
        }

        // rAthena `mapif_parse_broadcast` carries fontColor (uint32) + fontType (int16) +
        // fontSize/fontAlign/fontY. The proto only exposes color/type today; the remaining
        // fields default to 0 (rAthena's default for non-styled announcements).
        await _mapServerIpc.BroadcastAsync(new MapBroadcastNotification
        {
            SourceAccountId = request.SourceAccountId,
            SourceCharacterId = request.SourceCharacterId,
            SourceName = string.Empty,
            FontColor = (uint)Math.Max(request.Color, 0),
            FontType = (uint)Math.Max(request.Type, 0),
            Message = request.Message
        }, context.CancellationToken);

        return new InterBroadcastResponse { Success = true };
    }

    public override async Task<InterBroadcastItemResponse> InterBroadcastItem(
        InterBroadcastItemRequest request,
        ServerCallContext context)
    {
        if (request.ItemId == 0)
        {
            return new InterBroadcastItemResponse { Success = false };
        }

        await _mapServerIpc.BroadcastItemAsync(new MapItemBroadcastNotification
        {
            SourceAccountId = request.SourceAccountId,
            SourceCharacterId = request.SourceCharacterId,
            SourceName = string.Empty,
            ItemId = (uint)Math.Max(request.ItemId, 0),
            Amount = (uint)Math.Max(request.Amount, 0)
        }, context.CancellationToken);

        return new InterBroadcastItemResponse { Success = true };
    }

    public override async Task<InterWhisperResponse> InterWhisper(
        InterWhisperRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.SourceName)
            || string.IsNullOrWhiteSpace(request.TargetName)
            || string.IsNullOrWhiteSpace(request.Message))
        {
            return new InterWhisperResponse { Success = false, ErrorMessage = "Invalid whisper request" };
        }

        // Resolve target char id from name; rAthena lookup-by-name parity.
        var target = await _characterRepository.GetByNameAsync(request.TargetName, context.CancellationToken);
        if (target is null)
        {
            return new InterWhisperResponse { Success = false, ErrorMessage = "Recipient not found" };
        }

        var delivered = await _mapServerIpc.SendWhisperAsync(new MapWhisperNotification
        {
            TargetCharacterId = target.CharId,
            TargetName = target.Name,
            SourceName = request.SourceName,
            Message = request.Message
        }, context.CancellationToken);

        return new InterWhisperResponse
        {
            Success = delivered,
            ErrorMessage = delivered ? string.Empty : "Recipient not online"
        };
    }

    public override Task<InterWhisperReplyResponse> InterWhisperReply(
        InterWhisperReplyRequest request,
        ServerCallContext context)
    {
        // rAthena 0x3002 is the reply ack from the map server back to char (and on to the
        // requesting map server). The C# proto-shape uses the same RPC for the ack; since
        // SendWhisperAsync already aggregates per-map acks into a single delivered bool,
        // this RPC is currently a no-op acknowledgement endpoint for legacy callers.
        return Task.FromResult(new InterWhisperReplyResponse { Success = true });
    }

    public override async Task<InterWhisperToGmResponse> InterWhisperToGm(
        InterWhisperToGmRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.SourceName) || string.IsNullOrWhiteSpace(request.Message))
        {
            return new InterWhisperToGmResponse { Success = false };
        }

        await _mapServerIpc.SendWhisperToGmAsync(new MapWhisperToGmNotification
        {
            SourceName = request.SourceName,
            MinGroupId = (uint)Math.Max(request.MinGmLevel, 0),
            Message = request.Message
        }, context.CancellationToken);

        return new InterWhisperToGmResponse { Success = true };
    }

    public override async Task<InterRegistryUpdateResponse> InterRegistryUpdate(
        InterRegistryUpdateRequest request,
        ServerCallContext context)
    {
        var loginEntries = request.Entries.Select(entry => new GlobalAccRegEntry
        {
            Key = entry.Key,
            Index = entry.Index,
            IsNumeric = entry.IsNumeric,
            NumValue = entry.NumValue,
            StrValue = entry.StrValue
        });

        var result = await _loginServerIpc.UpdateGlobalAccountRegistersAsync(
            request.AccountId,
            loginEntries,
            context.CancellationToken);

        return new InterRegistryUpdateResponse
        {
            Success = result?.Success == true,
            UpdatedEntries = result?.UpdatedEntries ?? 0,
            ErrorMessage = result?.ErrorMessage ?? (result == null ? "Login server unavailable" : string.Empty)
        };
    }

    public override async Task<InterRegistryFetchResponse> InterRegistryFetch(
        InterRegistryFetchRequest request,
        ServerCallContext context)
    {
        var result = await _loginServerIpc.GetGlobalAccountRegistersAsync(
            request.AccountId,
            request.CharacterId,
            context.CancellationToken);

        var response = new InterRegistryFetchResponse
        {
            Success = result?.Success == true,
            ErrorMessage = result?.ErrorMessage ?? (result == null ? "Login server unavailable" : string.Empty)
        };

        if (result != null)
        {
            response.Entries.AddRange(result.Entries.Select(entry => new InterRegistryEntry
            {
                Key = entry.Key,
                Index = entry.Index,
                IsNumeric = entry.IsNumeric,
                NumValue = entry.NumValue,
                StrValue = entry.StrValue
            }));
        }

        return response;
    }

    public override async Task<InterNameChangeResponse> InterNameChange(
        InterNameChangeRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0 || string.IsNullOrWhiteSpace(request.NewName))
        {
            return new InterNameChangeResponse { Success = false, ErrorMessage = "Invalid name change request" };
        }

        // rAthena `mapif_parse_NameChangeRequest` (inter.cpp:1358) validates against
        // char_name_option / char_name_letters before acking. Option 1 = whitelist
        // (only listed letters allowed), 2 = blacklist (listed letters forbidden), else
        // no restriction.
        if (!IsAllowedCharName(request.NewName, _configuration.Char.CharNameOption, _configuration.Char.CharNameLetters))
        {
            return new InterNameChangeResponse { Success = false, ErrorMessage = "Name contains disallowed characters" };
        }

        // Broadcast to all map servers so they can refresh their UI / re-derive names.
        // rAthena leaves the DB update to the map server (TODO comment in inter.cpp:1384);
        // we mirror that — the map server's gameplay layer owns the rename DB write.
        await _mapServerIpc.NotifyNameChangeAsync(new MapNameChangeNotification
        {
            EntityType = request.RenameType,
            EntityId = request.CharacterId,
            NewName = request.NewName
        }, context.CancellationToken);

        return new InterNameChangeResponse { Success = true };
    }

    internal static bool IsAllowedCharName(string name, int option, string letters)
    {
        if (option == 1)
        {
            // whitelist
            return name.All(ch => letters.Contains(ch));
        }
        if (option == 2)
        {
            // blacklist
            return !name.Any(ch => letters.Contains(ch));
        }
        return true;
    }

    public override async Task<InterAccountInfoResponse> InterAccountInfo(
        InterAccountInfoRequest request,
        ServerCallContext context)
    {
        var info = await _loginServerIpc.RequestDetailedAccountInfoAsync(request.AccountId, context.CancellationToken);
        if (info?.Success != true)
        {
            return new InterAccountInfoResponse
            {
                Success = false,
                AccountId = request.AccountId
            };
        }

        return new InterAccountInfoResponse
        {
            Success = true,
            AccountId = (int)info.AccountId,
            Username = info.Username ?? string.Empty,
            Email = info.Email ?? string.Empty,
            GroupId = (uint)Math.Max(info.GroupId, 0),
            State = (uint)Math.Max(info.State, 0),
            LastIp = info.LastIp ?? string.Empty
        };
    }

    public override async Task<PartyCreateResponse> PartyCreate(
        PartyCreateRequest request,
        ServerCallContext context)
    {
        if (request.LeaderAccountId <= 0 || request.LeaderCharacterId <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return new PartyCreateResponse
            {
                Success = false,
                ErrorMessage = "Invalid party create request"
            };
        }

        var name = request.Name.Trim();
        if (name.Length == 0 || name.Length > 24)
        {
            return new PartyCreateResponse
            {
                Success = false,
                ErrorMessage = "Invalid party name"
            };
        }

        var leader = await _dbContext.Characters
            .FirstOrDefaultAsync(
                c => c.CharId == request.LeaderCharacterId && c.DeleteDate == 0,
                context.CancellationToken);
        if (leader is null || leader.AccountId != request.LeaderAccountId || leader.PartyId != 0)
        {
            return new PartyCreateResponse
            {
                Success = false,
                ErrorMessage = "Leader character is not eligible"
            };
        }

        var nameExists = await _dbContext.Parties
            .AsNoTracking()
            .AnyAsync(p => p.Name == name, context.CancellationToken);
        if (nameExists)
        {
            return new PartyCreateResponse
            {
                Success = false,
                ErrorMessage = "Party name already exists"
            };
        }

        var party = new PartyEntity
        {
            Name = name,
            Exp = 0,
            Item = (byte)Math.Clamp(request.Item, 0, byte.MaxValue),
            LeaderId = request.LeaderAccountId,
            LeaderChar = (int)request.LeaderCharacterId
        };
        _dbContext.Parties.Add(party);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        leader.PartyId = party.PartyId;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new PartyCreateResponse
        {
            Success = true,
            PartyId = party.PartyId
        };
    }

    public override async Task<PartyInfoResponse> PartyInfo(
        PartyInfoRequest request,
        ServerCallContext context)
    {
        var party = await LoadPartyInfoDataAsync(request.PartyId, context.CancellationToken);
        if (party is null)
        {
            return new PartyInfoResponse { Success = false };
        }

        return new PartyInfoResponse
        {
            Success = true,
            Party = party
        };
    }

    public override async Task<PartyAddMemberResponse> PartyAddMember(
        PartyAddMemberRequest request,
        ServerCallContext context)
    {
        if (request.PartyId <= 0 || request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return new PartyAddMemberResponse { Success = false };
        }

        var partyExists = await _dbContext.Parties
            .AsNoTracking()
            .AnyAsync(p => p.PartyId == request.PartyId, context.CancellationToken);
        if (!partyExists)
        {
            return new PartyAddMemberResponse { Success = false };
        }

        var character = await _dbContext.Characters
            .FirstOrDefaultAsync(
                c => c.CharId == request.CharacterId && c.DeleteDate == 0,
                context.CancellationToken);
        if (character is null || character.AccountId != request.AccountId)
        {
            return new PartyAddMemberResponse { Success = false };
        }

        if (character.PartyId != 0 && character.PartyId != request.PartyId)
        {
            return new PartyAddMemberResponse { Success = false };
        }

        character.PartyId = request.PartyId;
        if (request.ClassId > 0)
        {
            character.Class = (ushort)Math.Clamp(request.ClassId, 0, ushort.MaxValue);
        }
        if (request.Level > 0)
        {
            character.BaseLevel = (ushort)Math.Clamp(request.Level, 0, ushort.MaxValue);
        }
        if (!string.IsNullOrWhiteSpace(request.MapName))
        {
            character.LastMap = request.MapName;
        }
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new PartyAddMemberResponse { Success = true };
    }

    public override async Task<PartyChangeOptionResponse> PartyChangeOption(
        PartyChangeOptionRequest request,
        ServerCallContext context)
    {
        var party = await _dbContext.Parties
            .FirstOrDefaultAsync(p => p.PartyId == request.PartyId, context.CancellationToken);
        if (party is null)
        {
            return new PartyChangeOptionResponse { Success = false };
        }

        party.Exp = (byte)Math.Clamp(request.Exp, 0, byte.MaxValue);
        party.Item = (byte)Math.Clamp(request.Item, 0, byte.MaxValue);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new PartyChangeOptionResponse { Success = true };
    }

    public override async Task<PartyLeaveResponse> PartyLeave(
        PartyLeaveRequest request,
        ServerCallContext context)
    {
        var party = await _dbContext.Parties
            .FirstOrDefaultAsync(p => p.PartyId == request.PartyId, context.CancellationToken);
        if (party is null)
        {
            return new PartyLeaveResponse { Success = false };
        }

        var character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.CharId == request.CharacterId && c.DeleteDate == 0, context.CancellationToken);
        if (character is null || character.PartyId != request.PartyId)
        {
            return new PartyLeaveResponse { Success = false };
        }

        // rAthena `mapif_parse_PartyLeave` (int_party.cpp:633): if the leader leaves,
        // the entire party is disbanded (calls mapif_parse_BreakParty internally). All
        // remaining members' party_id is cleared and the party row is removed.
        if (party.LeaderChar == request.CharacterId)
        {
            var allMembers = await _dbContext.Characters
                .Where(c => c.PartyId == request.PartyId)
                .ToListAsync(context.CancellationToken);
            foreach (var member in allMembers)
            {
                member.PartyId = 0;
            }
            _dbContext.Parties.Remove(party);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            return new PartyLeaveResponse { Success = true };
        }

        // Non-leader: remove only that member.
        character.PartyId = 0;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        // If that was the last non-leader member AND the party is somehow empty (shouldn't
        // happen given leader still present, but defensive), clean up the row.
        var anyLeft = await _dbContext.Characters
            .AsNoTracking()
            .AnyAsync(c => c.PartyId == request.PartyId && c.DeleteDate == 0, context.CancellationToken);
        if (!anyLeft)
        {
            _dbContext.Parties.Remove(party);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }

        return new PartyLeaveResponse { Success = true };
    }

    public override async Task<PartyChangeMapResponse> PartyChangeMap(
        PartyChangeMapRequest request,
        ServerCallContext context)
    {
        var character = await _dbContext.Characters
            .FirstOrDefaultAsync(
                c => c.CharId == request.CharacterId && c.PartyId == request.PartyId && c.DeleteDate == 0,
                context.CancellationToken);
        if (character is null)
        {
            return new PartyChangeMapResponse { Success = false };
        }

        character.Online = request.Online ? (short)1 : (short)0;
        if (request.Level > 0)
        {
            character.BaseLevel = (ushort)Math.Clamp(request.Level, 0, ushort.MaxValue);
        }
        if (!string.IsNullOrWhiteSpace(request.MapName))
        {
            character.LastMap = request.MapName;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new PartyChangeMapResponse { Success = true };
    }

    public override async Task<PartyBreakResponse> PartyBreak(
        PartyBreakRequest request,
        ServerCallContext context)
    {
        var party = await _dbContext.Parties
            .FirstOrDefaultAsync(p => p.PartyId == request.PartyId, context.CancellationToken);
        if (party is null)
        {
            return new PartyBreakResponse { Success = false };
        }

        var members = await _dbContext.Characters
            .Where(c => c.PartyId == request.PartyId)
            .ToListAsync(context.CancellationToken);
        foreach (var member in members)
        {
            member.PartyId = 0;
        }

        _dbContext.Parties.Remove(party);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new PartyBreakResponse { Success = true };
    }

    public override async Task<PartyMessageResponse> PartyMessage(
        PartyMessageRequest request,
        ServerCallContext context)
    {
        var exists = await _dbContext.Parties
            .AsNoTracking()
            .AnyAsync(p => p.PartyId == request.PartyId, context.CancellationToken);
        var success = exists && !string.IsNullOrWhiteSpace(request.Message);
        return new PartyMessageResponse { Success = success };
    }

    public override async Task<PartyLeaderChangeResponse> PartyLeaderChange(
        PartyLeaderChangeRequest request,
        ServerCallContext context)
    {
        var party = await _dbContext.Parties
            .FirstOrDefaultAsync(p => p.PartyId == request.PartyId, context.CancellationToken);
        if (party is null)
        {
            return new PartyLeaderChangeResponse { Success = false };
        }

        var character = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CharId == request.CharacterId && c.PartyId == request.PartyId, context.CancellationToken);
        if (character is null)
        {
            return new PartyLeaderChangeResponse { Success = false };
        }

        party.LeaderChar = character.CharId;
        party.LeaderId = character.AccountId;
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new PartyLeaderChangeResponse { Success = true };
    }

    public override Task<PartyShareLevelResponse> PartyShareLevel(
        PartyShareLevelRequest request,
        ServerCallContext context)
    {
        _charServer.SetPartyShareLevel(request.ShareLevel);
        return Task.FromResult(new PartyShareLevelResponse { Success = true });
    }

    public override async Task<GuildCreateResponse> GuildCreate(
        GuildCreateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.MasterCharacterId <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return new GuildCreateResponse
            {
                Success = false,
                ErrorMessage = "Invalid guild create request"
            };
        }

        var guildName = request.Name.Trim();
        if (guildName.Length == 0 || guildName.Length > 24)
        {
            return new GuildCreateResponse
            {
                Success = false,
                ErrorMessage = "Invalid guild name"
            };
        }

        var existingByName = await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(g => g.Name == guildName, context.CancellationToken);
        if (existingByName)
        {
            return new GuildCreateResponse
            {
                Success = false,
                ErrorMessage = "Guild name already exists"
            };
        }

        var masterChar = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.CharId == request.MasterCharacterId && c.DeleteDate == 0, context.CancellationToken);
        if (masterChar is null || masterChar.AccountId != request.AccountId || masterChar.GuildId != 0)
        {
            return new GuildCreateResponse
            {
                Success = false,
                ErrorMessage = "Master character is not eligible"
            };
        }

        var guild = new GuildEntity
        {
            Name = guildName,
            CharId = masterChar.CharId,
            Master = masterChar.Name,
            GuildLv = 1,
            MaxMember = 16,
            AverageLv = (ushort)Math.Clamp((int)masterChar.BaseLevel, 1, ushort.MaxValue),
            Mes1 = string.Empty,
            Mes2 = string.Empty
        };

        _dbContext.Guilds.Add(guild);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _dbContext.GuildMembers.Add(new GuildMemberEntity
        {
            GuildId = guild.GuildId,
            CharId = masterChar.CharId,
            Position = 0
        });
        masterChar.GuildId = guild.GuildId;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new GuildCreateResponse
        {
            Success = true,
            GuildId = guild.GuildId
        };
    }

    public override async Task<GuildInfoResponse> GuildInfo(
        GuildInfoRequest request,
        ServerCallContext context)
    {
        var data = await LoadGuildInfoDataAsync(request.GuildId, context.CancellationToken);
        if (data is null)
        {
            return new GuildInfoResponse { Success = false };
        }

        return new GuildInfoResponse
        {
            Success = true,
            Guild = data
        };
    }

    public override async Task<GuildAddMemberResponse> GuildAddMember(
        GuildAddMemberRequest request,
        ServerCallContext context)
    {
        if (request.GuildId <= 0 || request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return new GuildAddMemberResponse { Success = false };
        }

        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildAddMemberResponse { Success = false };
        }

        var charEntity = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.CharId == request.CharacterId && c.DeleteDate == 0, context.CancellationToken);
        if (charEntity is null || charEntity.AccountId != request.AccountId)
        {
            return new GuildAddMemberResponse { Success = false };
        }

        if (charEntity.GuildId != 0 && charEntity.GuildId != request.GuildId)
        {
            return new GuildAddMemberResponse { Success = false };
        }

        var memberCount = await _dbContext.GuildMembers
            .AsNoTracking()
            .CountAsync(m => m.GuildId == request.GuildId, context.CancellationToken);
        if (guild.MaxMember > 0 && memberCount >= guild.MaxMember)
        {
            return new GuildAddMemberResponse { Success = false };
        }

        var memberExists = await _dbContext.GuildMembers
            .AsNoTracking()
            .AnyAsync(m => m.GuildId == request.GuildId && m.CharId == request.CharacterId, context.CancellationToken);
        if (!memberExists)
        {
            _dbContext.GuildMembers.Add(new GuildMemberEntity
            {
                CharId = (int)request.CharacterId,
                GuildId = request.GuildId,
                Position = 1
            });
        }

        charEntity.GuildId = request.GuildId;
        if (request.Level > 0)
        {
            charEntity.BaseLevel = (ushort)Math.Clamp(request.Level, 1, ushort.MaxValue);
        }
        if (request.ClassId > 0)
        {
            charEntity.Class = (ushort)Math.Clamp(request.ClassId, 0, ushort.MaxValue);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildAddMemberResponse { Success = true };
    }

    public override async Task<GuildMasterChangeResponse> GuildMasterChange(
        GuildMasterChangeRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildMasterChangeResponse { Success = false };
        }

        var newMaster = await _dbContext.Characters
            .FirstOrDefaultAsync(
                c => c.GuildId == request.GuildId && c.DeleteDate == 0 &&
                     c.Name == request.MasterName,
                context.CancellationToken);
        if (newMaster is null)
        {
            return new GuildMasterChangeResponse { Success = false };
        }

        var oldMasterMember = await _dbContext.GuildMembers
            .FirstOrDefaultAsync(m => m.GuildId == request.GuildId && m.CharId == guild.CharId, context.CancellationToken);
        var newMasterMember = await _dbContext.GuildMembers
            .FirstOrDefaultAsync(m => m.GuildId == request.GuildId && m.CharId == newMaster.CharId, context.CancellationToken);
        if (newMasterMember is null)
        {
            return new GuildMasterChangeResponse { Success = false };
        }

        if (oldMasterMember is not null && oldMasterMember.CharId != newMasterMember.CharId)
        {
            oldMasterMember.Position = 1;
        }
        newMasterMember.Position = 0;
        guild.CharId = newMaster.CharId;
        guild.Master = newMaster.Name;
        guild.LastMasterChange = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildMasterChangeResponse { Success = true };
    }

    public override async Task<GuildLeaveResponse> GuildLeave(
        GuildLeaveRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildLeaveResponse { Success = false };
        }

        var member = await _dbContext.GuildMembers
            .FirstOrDefaultAsync(m => m.GuildId == request.GuildId && m.CharId == request.CharacterId, context.CancellationToken);
        var charEntity = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.CharId == request.CharacterId && c.GuildId == request.GuildId, context.CancellationToken);
        if (member is null || charEntity is null)
        {
            return new GuildLeaveResponse { Success = false };
        }

        _dbContext.GuildMembers.Remove(member);
        charEntity.GuildId = 0;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        var remaining = await _dbContext.GuildMembers
            .AsNoTracking()
            .Where(m => m.GuildId == request.GuildId)
            .OrderBy(m => m.Position)
            .ThenBy(m => m.CharId)
            .ToListAsync(context.CancellationToken);

        if (remaining.Count == 0)
        {
            _dbContext.Guilds.Remove(guild);
        }
        else if (guild.CharId == request.CharacterId)
        {
            var newMasterMember = remaining[0];
            var newMasterChar = await _dbContext.Characters
                .FirstOrDefaultAsync(c => c.CharId == newMasterMember.CharId, context.CancellationToken);
            if (newMasterChar is not null)
            {
                var mutableMasterMember = await _dbContext.GuildMembers
                    .FirstOrDefaultAsync(m => m.GuildId == request.GuildId && m.CharId == newMasterMember.CharId, context.CancellationToken);
                if (mutableMasterMember is not null)
                {
                    mutableMasterMember.Position = 0;
                }

                guild.CharId = newMasterChar.CharId;
                guild.Master = newMasterChar.Name;
                guild.LastMasterChange = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildLeaveResponse { Success = true };
    }

    public override async Task<GuildChangeMemberInfoShortResponse> GuildChangeMemberInfoShort(
        GuildChangeMemberInfoShortRequest request,
        ServerCallContext context)
    {
        var memberExists = await _dbContext.GuildMembers
            .AsNoTracking()
            .AnyAsync(m => m.GuildId == request.GuildId && m.CharId == request.CharacterId, context.CancellationToken);
        if (!memberExists)
        {
            return new GuildChangeMemberInfoShortResponse { Success = false };
        }

        var charEntity = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.CharId == request.CharacterId && c.GuildId == request.GuildId, context.CancellationToken);
        if (charEntity is null)
        {
            return new GuildChangeMemberInfoShortResponse { Success = false };
        }

        charEntity.Online = request.Online ? (short)1 : (short)0;
        if (request.Level > 0)
        {
            charEntity.BaseLevel = (ushort)Math.Clamp(request.Level, 1, ushort.MaxValue);
        }
        if (request.ClassId > 0)
        {
            charEntity.Class = (ushort)Math.Clamp(request.ClassId, 0, ushort.MaxValue);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildChangeMemberInfoShortResponse { Success = true };
    }

    public override async Task<GuildBreakResponse> GuildBreak(
        GuildBreakRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildBreakResponse { Success = false };
        }

        // rAthena `mapif_parse_BreakGuild` (int_guild.cpp): cascades cleanup across
        // all guild-related tables so guild_id can be safely reused without orphaned
        // rows returning stale state on the next gameplay query.
        var members = await _dbContext.Characters
            .Where(c => c.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        foreach (var member in members)
        {
            member.GuildId = 0;
        }

        var guildMembers = await _dbContext.GuildMembers
            .Where(m => m.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        if (guildMembers.Count > 0) _dbContext.GuildMembers.RemoveRange(guildMembers);

        var positions = await _dbContext.GuildPositions
            .Where(p => p.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        if (positions.Count > 0) _dbContext.GuildPositions.RemoveRange(positions);

        var skills = await _dbContext.GuildSkills
            .Where(s => s.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        if (skills.Count > 0) _dbContext.GuildSkills.RemoveRange(skills);

        var alliances = await _dbContext.GuildAlliances
            .Where(a => a.GuildId == request.GuildId || a.AllianceId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        if (alliances.Count > 0) _dbContext.GuildAlliances.RemoveRange(alliances);

        var expulsions = await _dbContext.GuildExpulsions
            .Where(e => e.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        if (expulsions.Count > 0) _dbContext.GuildExpulsions.RemoveRange(expulsions);

        var castles = await _dbContext.GuildCastles
            .Where(c => c.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        // Castles aren't deleted on guild break — rAthena clears guild_id and resets
        // the castle so any future guild can capture it.
        foreach (var castle in castles)
        {
            castle.GuildId = 0;
        }

        var storagePayloads = await _dbContext.GuildStoragePayloads
            .Where(s => s.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        if (storagePayloads.Count > 0) _dbContext.GuildStoragePayloads.RemoveRange(storagePayloads);

        _dbContext.Guilds.Remove(guild);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildBreakResponse { Success = true };
    }

    public override async Task<GuildMessageResponse> GuildMessage(
        GuildMessageRequest request,
        ServerCallContext context)
    {
        var guildExists = await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        var success = guildExists && !string.IsNullOrWhiteSpace(request.Message);
        return new GuildMessageResponse { Success = success };
    }

    public override async Task<GuildBasicInfoChangeResponse> GuildBasicInfoChange(
        GuildBasicInfoChangeRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildBasicInfoChangeResponse { Success = false };
        }

        switch (request.Type)
        {
            case 1:
                var newName = request.Data.ToStringUtf8().Trim();
                if (newName.Length == 0 || newName.Length > 24)
                {
                    return new GuildBasicInfoChangeResponse { Success = false };
                }

                var nameConflict = await _dbContext.Guilds
                    .AsNoTracking()
                    .AnyAsync(g => g.GuildId != request.GuildId && g.Name == newName, context.CancellationToken);
                if (nameConflict)
                {
                    return new GuildBasicInfoChangeResponse { Success = false };
                }
                guild.Name = newName;
                break;
            case 2:
                if (request.Data.Length >= 4)
                {
                    guild.GuildLv = (byte)Math.Clamp(BitConverter.ToInt32(request.Data.ToByteArray(), 0), 0, byte.MaxValue);
                }
                break;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildBasicInfoChangeResponse { Success = true };
    }

    public override async Task<GuildMemberInfoChangeResponse> GuildMemberInfoChange(
        GuildMemberInfoChangeRequest request,
        ServerCallContext context)
    {
        var member = await _dbContext.GuildMembers
            .FirstOrDefaultAsync(m => m.GuildId == request.GuildId && m.CharId == request.CharacterId, context.CancellationToken);
        if (member is null)
        {
            return new GuildMemberInfoChangeResponse { Success = false };
        }

        switch (request.Type)
        {
            case 1:
                var position = await _dbContext.GuildPositions
                    .FirstOrDefaultAsync(p => p.GuildId == request.GuildId && p.Position == member.Position, context.CancellationToken);
                var posName = request.Data.ToStringUtf8();
                if (position is null)
                {
                    _dbContext.GuildPositions.Add(new GuildPositionEntity
                    {
                        GuildId = request.GuildId,
                        Position = member.Position,
                        Name = posName
                    });
                }
                else
                {
                    position.Name = posName;
                }
                break;
            case 2:
                if (request.Data.Length >= 4)
                {
                    var classId = BitConverter.ToInt32(request.Data.ToByteArray(), 0);
                    var charEntity = await _dbContext.Characters
                        .FirstOrDefaultAsync(c => c.CharId == request.CharacterId && c.GuildId == request.GuildId, context.CancellationToken);
                    if (charEntity is not null)
                    {
                        charEntity.Class = (ushort)Math.Clamp(classId, 0, ushort.MaxValue);
                    }
                }
                break;
            case 3:
                // GMI_EXP — member EXP set. rAthena int_guild.cpp:1555:
                //   delta = new_exp - old_exp
                //   guild_total_exp += delta * guild_exp_rate / 100
                if (request.Data.Length >= 8)
                {
                    var newExp = BitConverter.ToUInt64(request.Data.ToByteArray(), 0);
                    var oldExp = member.Exp;
                    member.Exp = newExp;
                    if (newExp > oldExp)
                    {
                        var delta = newExp - oldExp;
                        if (_configuration.GuildExpRate != 100 && _configuration.GuildExpRate >= 0)
                        {
                            delta = delta * (ulong)_configuration.GuildExpRate / 100UL;
                        }
                        var guild = await _dbContext.Guilds
                            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
                        if (guild is not null)
                        {
                            // safe_addition_cap to MAX_GUILD_EXP (rAthena: uint64 max).
                            var sum = guild.Exp + delta;
                            guild.Exp = sum < guild.Exp ? ulong.MaxValue : sum;
                        }
                    }
                }
                break;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildMemberInfoChangeResponse { Success = true };
    }

    public override async Task<GuildPositionChangeResponse> GuildPositionChange(
        GuildPositionChangeRequest request,
        ServerCallContext context)
    {
        var guildExists = await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (!guildExists)
        {
            return new GuildPositionChangeResponse { Success = false };
        }

        var index = (byte)Math.Clamp(request.Index, 0, byte.MaxValue);
        var position = await _dbContext.GuildPositions
            .FirstOrDefaultAsync(p => p.GuildId == request.GuildId && p.Position == index, context.CancellationToken);
        if (position is null)
        {
            position = new GuildPositionEntity
            {
                GuildId = request.GuildId,
                Position = index,
                Name = request.Position?.Name ?? string.Empty,
                Mode = (ushort)Math.Clamp(request.Position?.Mode ?? 0, 0, ushort.MaxValue),
                ExpMode = (byte)Math.Clamp(request.Position?.ExpMode ?? 0, 0, byte.MaxValue)
            };
            _dbContext.GuildPositions.Add(position);
        }
        else
        {
            position.Name = request.Position?.Name ?? string.Empty;
            position.Mode = (ushort)Math.Clamp(request.Position?.Mode ?? 0, 0, ushort.MaxValue);
            position.ExpMode = (byte)Math.Clamp(request.Position?.ExpMode ?? 0, 0, byte.MaxValue);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildPositionChangeResponse { Success = true };
    }

    public override async Task<GuildSkillUpResponse> GuildSkillUp(
        GuildSkillUpRequest request,
        ServerCallContext context)
    {
        var guildExists = await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (!guildExists)
        {
            return new GuildSkillUpResponse { Success = false };
        }

        var skillId = (ushort)Math.Clamp((int)request.SkillId, 0, ushort.MaxValue);
        var skill = await _dbContext.GuildSkills
            .FirstOrDefaultAsync(s => s.GuildId == request.GuildId && s.Id == skillId, context.CancellationToken);
        var max = (byte)Math.Clamp(request.Max, 0, byte.MaxValue);
        if (skill is null)
        {
            _dbContext.GuildSkills.Add(new GuildSkillEntity
            {
                GuildId = request.GuildId,
                Id = skillId,
                Lv = max == 0 ? (byte)0 : (byte)1
            });
        }
        else
        {
            var nextLv = skill.Lv + 1;
            skill.Lv = max == 0 ? (byte)0 : (byte)Math.Min(nextLv, max);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildSkillUpResponse { Success = true };
    }

    public override async Task<GuildAllianceResponse> GuildAlliance(
        GuildAllianceRequest request,
        ServerCallContext context)
    {
        var guild1 = await _dbContext.Guilds.FirstOrDefaultAsync(g => g.GuildId == request.GuildId1, context.CancellationToken);
        var guild2 = await _dbContext.Guilds.FirstOrDefaultAsync(g => g.GuildId == request.GuildId2, context.CancellationToken);
        if (guild1 is null || guild2 is null)
        {
            return new GuildAllianceResponse { Success = false };
        }

        if (request.Flag == 0)
        {
            var links = await _dbContext.GuildAlliances
                .Where(a =>
                    (a.GuildId == request.GuildId1 && a.AllianceId == request.GuildId2) ||
                    (a.GuildId == request.GuildId2 && a.AllianceId == request.GuildId1))
                .ToListAsync(context.CancellationToken);
            if (links.Count > 0)
            {
                _dbContext.GuildAlliances.RemoveRange(links);
            }
        }
        else
        {
            await UpsertGuildAllianceAsync(request.GuildId1, request.GuildId2, request.Flag, guild2.Name, context.CancellationToken);
            await UpsertGuildAllianceAsync(request.GuildId2, request.GuildId1, request.Flag, guild1.Name, context.CancellationToken);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildAllianceResponse { Success = true };
    }

    public override async Task<GuildNoticeResponse> GuildNotice(
        GuildNoticeRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildNoticeResponse { Success = false };
        }

        guild.Mes1 = Truncate(request.Notice1 ?? string.Empty, 60);
        guild.Mes2 = Truncate(request.Notice2 ?? string.Empty, 120);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new GuildNoticeResponse { Success = true };
    }

    public override async Task<GuildEmblemResponse> GuildEmblem(
        GuildEmblemRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildEmblemResponse { Success = false };
        }

        var data = request.Data.ToByteArray();
        guild.EmblemData = data;
        guild.EmblemLen = (uint)data.Length;
        guild.EmblemId = guild.EmblemId + 1;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new GuildEmblemResponse { Success = true };
    }

    public override async Task<GuildCastleDataLoadResponse> GuildCastleDataLoad(
        GuildCastleDataLoadRequest request,
        ServerCallContext context)
    {
        var response = new GuildCastleDataLoadResponse { Success = true };
        foreach (var castleId in request.CastleIds.Where(id => id > 0).Distinct())
        {
            var castle = await _dbContext.GuildCastles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CastleId == castleId, context.CancellationToken);

            for (var index = 1; index <= 17; index++)
            {
                response.Values[(castleId << 8) + index] = GetGuildCastleIndexValue(castle, index);
            }
        }

        return response;
    }

    public override async Task<GuildCastleDataSaveResponse> GuildCastleDataSave(
        GuildCastleDataSaveRequest request,
        ServerCallContext context)
    {
        if (request.CastleId <= 0 || request.Index <= 0)
        {
            return new GuildCastleDataSaveResponse { Success = false };
        }

        var castle = await _dbContext.GuildCastles
            .FirstOrDefaultAsync(c => c.CastleId == request.CastleId, context.CancellationToken);
        if (castle is null)
        {
            castle = new GuildCastleEntity { CastleId = request.CastleId };
            _dbContext.GuildCastles.Add(castle);
        }

        if (!TrySetGuildCastleIndexValue(castle, request.Index, request.Value))
        {
            return new GuildCastleDataSaveResponse { Success = false };
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildCastleDataSaveResponse { Success = true };
    }

    public override async Task<GuildEmblemVersionResponse> GuildEmblemVersion(
        GuildEmblemVersionRequest request,
        ServerCallContext context)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == request.GuildId, context.CancellationToken);
        if (guild is null)
        {
            return new GuildEmblemVersionResponse { Success = false };
        }

        guild.EmblemId = (uint)Math.Max(request.Version, 0);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new GuildEmblemVersionResponse { Success = true };
    }

    public override async Task<GuildStorageLoadResponse> GuildStorageLoad(
        GuildStorageLoadRequest request,
        ServerCallContext context)
    {
        if (request.GuildId <= 0)
        {
            return new GuildStorageLoadResponse { Success = false };
        }

        var row = await _dbContext.GuildStoragePayloads
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GuildId == request.GuildId, context.CancellationToken);

        return new GuildStorageLoadResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(row?.Data ?? Array.Empty<byte>())
        };
    }

    public override async Task<GuildStorageSaveResponse> GuildStorageSave(
        GuildStorageSaveRequest request,
        ServerCallContext context)
    {
        if (request.GuildId <= 0)
        {
            return new GuildStorageSaveResponse { Success = false };
        }

        var data = request.Data.ToByteArray();
        var row = await _dbContext.GuildStoragePayloads
            .FirstOrDefaultAsync(s => s.GuildId == request.GuildId, context.CancellationToken);
        if (row is null)
        {
            _dbContext.GuildStoragePayloads.Add(new GuildStoragePayloadEntity
            {
                GuildId = request.GuildId,
                Data = data
            });
        }
        else
        {
            row.Data = data;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new GuildStorageSaveResponse { Success = true };
    }

    public override Task<StorageItemboundRetrieveResponse> StorageItemboundRetrieve(
        StorageItemboundRetrieveRequest request,
        ServerCallContext context)
    {
        var success = request.AccountId > 0 && request.CharacterId > 0;
        return Task.FromResult(new StorageItemboundRetrieveResponse
        {
            Success = success,
            RetrievedCount = success ? 0 : 0
        });
    }

    public override async Task<AccountStorageLoadResponse> AccountStorageLoad(
        AccountStorageLoadRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return new AccountStorageLoadResponse { Success = false };
        }

        var row = await _dbContext.AccountStoragePayloads
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.AccountId == request.AccountId, context.CancellationToken);

        return new AccountStorageLoadResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(row?.Data ?? Array.Empty<byte>())
        };
    }

    public override async Task<AccountStorageSaveResponse> AccountStorageSave(
        AccountStorageSaveRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return new AccountStorageSaveResponse { Success = false };
        }

        var data = request.Data.ToByteArray();
        var row = await _dbContext.AccountStoragePayloads
            .FirstOrDefaultAsync(s => s.AccountId == request.AccountId, context.CancellationToken);
        if (row is null)
        {
            _dbContext.AccountStoragePayloads.Add(new AccountStoragePayloadEntity
            {
                AccountId = request.AccountId,
                Data = data
            });
        }
        else
        {
            row.Data = data;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new AccountStorageSaveResponse { Success = true };
    }

    public override async Task<MailRequestInboxResponse> MailRequestInbox(
        MailRequestInboxRequest request,
        ServerCallContext context)
    {
        var response = new MailRequestInboxResponse { Success = request.CharacterId > 0 };
        if (request.CharacterId <= 0)
        {
            return response;
        }

        var mails = await _dbContext.Mails
            .AsNoTracking()
            .Where(m => m.DestId == request.CharacterId)
            .OrderByDescending(m => m.Id)
            .ToListAsync(context.CancellationToken);
        var charIds = mails
            .SelectMany(m => new[] { m.SendId, m.DestId })
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var accountsByCharId = charIds.Count == 0
            ? new Dictionary<int, int>()
            : await _dbContext.Characters
                .AsNoTracking()
                .Where(c => charIds.Contains(c.CharId))
                .ToDictionaryAsync(c => c.CharId, c => c.AccountId, context.CancellationToken);

        var mailIds = mails.Select(m => m.Id).ToList();
        var attachmentsByMailId = mailIds.Count == 0
            ? new Dictionary<long, List<MailAttachmentEntity>>()
            : (await _dbContext.MailAttachments
                .AsNoTracking()
                .Where(a => mailIds.Contains(a.Id))
                .ToListAsync(context.CancellationToken))
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var mail in mails)
        {
            var attachments = attachmentsByMailId.TryGetValue(mail.Id, out var list)
                ? (IEnumerable<MailAttachmentEntity>)list
                : Array.Empty<MailAttachmentEntity>();
            response.Mails.Add(ToMailMessageData(mail, accountsByCharId, attachments));
        }

        return response;
    }

    public override async Task<MailReadResponse> MailRead(
        MailReadRequest request,
        ServerCallContext context)
    {
        var mail = await _dbContext.Mails
            .FirstOrDefaultAsync(
                m => m.Id == request.MailId && m.DestId == request.CharacterId,
                context.CancellationToken);
        if (mail is null)
        {
            return new MailReadResponse { Success = false };
        }

        mail.Status = 1;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        var accountsByCharId = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.CharId == mail.SendId || c.CharId == mail.DestId)
            .ToDictionaryAsync(c => c.CharId, c => c.AccountId, context.CancellationToken);

        var attachments = await _dbContext.MailAttachments
            .AsNoTracking()
            .Where(a => a.Id == mail.Id)
            .ToListAsync(context.CancellationToken);

        return new MailReadResponse
        {
            Success = true,
            Mail = ToMailMessageData(mail, accountsByCharId, attachments)
        };
    }

    public override async Task<MailGetAttachmentResponse> MailGetAttachment(
        MailGetAttachmentRequest request,
        ServerCallContext context)
    {
        var mail = await _dbContext.Mails
            .FirstOrDefaultAsync(
                m => m.Id == request.MailId && m.DestId == request.CharacterId,
                context.CancellationToken);
        if (mail is null)
        {
            return new MailGetAttachmentResponse { Success = false };
        }
        // rAthena int_mail.cpp:385 — when mail_retrieve is off, the user
        // must read the mail before they can claim attachments.
        // status: 0 = MAIL_NEW, 1 = MAIL_UNREAD, 2 = MAIL_READ.
        if (_configuration.MailRetrieve == 0 && mail.Status != 2)
        {
            return new MailGetAttachmentResponse { Success = false };
        }

        var zeny = mail.Zeny;
        mail.Zeny = 0;

        var attachments = await _dbContext.MailAttachments
            .Where(a => a.Id == request.MailId)
            .ToListAsync(context.CancellationToken);
        if (attachments.Count > 0)
        {
            _dbContext.MailAttachments.RemoveRange(attachments);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        var response = new MailGetAttachmentResponse
        {
            Success = true,
            Zeny = zeny,
            Attachment = Google.Protobuf.ByteString.Empty
        };
        foreach (var att in attachments)
        {
            response.Items.Add(ToMailAttachmentItem(att));
        }
        return response;
    }

    public override async Task<MailDeleteResponse> MailDelete(
        MailDeleteRequest request,
        ServerCallContext context)
    {
        var mail = await _dbContext.Mails
            .FirstOrDefaultAsync(
                m => m.Id == request.MailId && m.DestId == request.CharacterId,
                context.CancellationToken);
        if (mail is null)
        {
            return new MailDeleteResponse { Success = false };
        }

        var attachments = await _dbContext.MailAttachments
            .Where(a => a.Id == request.MailId)
            .ToListAsync(context.CancellationToken);
        if (attachments.Count > 0)
        {
            _dbContext.MailAttachments.RemoveRange(attachments);
        }

        _dbContext.Mails.Remove(mail);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new MailDeleteResponse { Success = true };
    }

    public override async Task<MailReturnResponse> MailReturn(
        MailReturnRequest request,
        ServerCallContext context)
    {
        var mail = await _dbContext.Mails
            .FirstOrDefaultAsync(
                m => m.Id == request.MailId && m.DestId == request.CharacterId,
                context.CancellationToken);
        if (mail is null)
        {
            return new MailReturnResponse { Success = false };
        }

        var returned = new MailEntity
        {
            SendId = mail.DestId,
            SendName = mail.DestName,
            DestId = mail.SendId,
            DestName = mail.SendName,
            Title = $"RE: {mail.Title}",
            Message = mail.Message,
            Zeny = mail.Zeny,
            Time = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Status = 0,
            Type = mail.Type
        };
        _dbContext.Mails.Add(returned);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        var attachments = await _dbContext.MailAttachments
            .AsNoTracking()
            .Where(a => a.Id == mail.Id)
            .ToListAsync(context.CancellationToken);
        if (attachments.Count > 0)
        {
            _dbContext.MailAttachments.AddRange(attachments.Select(a => new MailAttachmentEntity
            {
                Id = returned.Id,
                Index = a.Index,
                NameId = a.NameId,
                Amount = a.Amount,
                Refine = a.Refine,
                Attribute = a.Attribute,
                Identify = a.Identify,
                Card0 = a.Card0,
                Card1 = a.Card1,
                Card2 = a.Card2,
                Card3 = a.Card3,
                OptionId0 = a.OptionId0,
                OptionVal0 = a.OptionVal0,
                OptionParm0 = a.OptionParm0,
                OptionId1 = a.OptionId1,
                OptionVal1 = a.OptionVal1,
                OptionParm1 = a.OptionParm1,
                OptionId2 = a.OptionId2,
                OptionVal2 = a.OptionVal2,
                OptionParm2 = a.OptionParm2,
                OptionId3 = a.OptionId3,
                OptionVal3 = a.OptionVal3,
                OptionParm3 = a.OptionParm3,
                OptionId4 = a.OptionId4,
                OptionVal4 = a.OptionVal4,
                OptionParm4 = a.OptionParm4,
                UniqueId = a.UniqueId,
                Bound = a.Bound,
                EnchantGrade = a.EnchantGrade
            }));
            _dbContext.MailAttachments.RemoveRange(
                await _dbContext.MailAttachments
                    .Where(a => a.Id == mail.Id)
                    .ToListAsync(context.CancellationToken));
        }

        _dbContext.Mails.Remove(mail);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new MailReturnResponse { Success = true };
    }

    public override async Task<MailSendResponse> MailSend(
        MailSendRequest request,
        ServerCallContext context)
    {
        if (request.ReceiverCharacterId <= 0 || string.IsNullOrWhiteSpace(request.ReceiverName))
        {
            return new MailSendResponse { Success = false };
        }

        var mail = new MailEntity
        {
            SendId = (int)request.SenderCharacterId,
            SendName = Truncate(request.SenderName ?? string.Empty, 30),
            DestId = (int)request.ReceiverCharacterId,
            DestName = Truncate(request.ReceiverName ?? string.Empty, 30),
            Title = Truncate(request.Title ?? string.Empty, 45),
            Message = Truncate(request.Body ?? string.Empty, 500),
            Time = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Status = 0,
            Zeny = SafeUIntFromLong(request.Zeny),
            Type = 0
        };
        _dbContext.Mails.Add(mail);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        if (request.Items.Count > 0)
        {
            foreach (var item in request.Items)
            {
                if (item.NameId == 0 || item.Amount == 0)
                {
                    continue;
                }
                _dbContext.MailAttachments.Add(ToMailAttachmentEntity(mail.Id, item));
            }
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }

        return new MailSendResponse
        {
            Success = true,
            MailId = mail.Id
        };
    }

    public override async Task<MailReceiverCheckResponse> MailReceiverCheck(
        MailReceiverCheckRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiverName))
        {
            return new MailReceiverCheckResponse
            {
                Success = false,
                AccountId = 0,
                CharacterId = 0,
                ReceiverName = string.Empty
            };
        }

        var receiver = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.DeleteDate == 0 && c.Name == request.ReceiverName,
                context.CancellationToken);
        if (receiver is null)
        {
            return new MailReceiverCheckResponse
            {
                Success = false,
                AccountId = 0,
                CharacterId = 0,
                ReceiverName = request.ReceiverName
            };
        }

        return new MailReceiverCheckResponse
        {
            Success = true,
            AccountId = receiver.AccountId,
            CharacterId = receiver.CharId,
            ReceiverName = receiver.Name
        };
    }

    public override async Task<AuctionRequestListResponse> AuctionRequestList(
        AuctionRequestListRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new AuctionRequestListResponse { Success = false, Count = 0, Pages = 0 };
        }

        var requestedPage = Math.Max(1, request.Page);
        var filtered = await _dbContext.Auctions
            .AsNoTracking()
            .Where(auction => MatchesAuctionRequest(auction, request))
            .OrderByDescending(auction => auction.AuctionId)
            .ToListAsync(context.CancellationToken);

        var pages = Math.Max(1, (int)Math.Ceiling(filtered.Count / 5.0));
        var page = Math.Min(requestedPage, pages);
        var pageItems = filtered.Skip((page - 1) * 5).Take(5).ToList();

        var response = new AuctionRequestListResponse
        {
            Success = true,
            Count = pageItems.Count,
            Pages = pages
        };
        response.Auctions.AddRange(pageItems.Select(ToAuctionData));
        return response;
    }

    public override async Task<AuctionRegisterResponse> AuctionRegister(
        AuctionRegisterRequest request,
        ServerCallContext context)
    {
        if (request.Auction == null || request.Auction.SellerCharacterId <= 0)
        {
            return new AuctionRegisterResponse { Success = false };
        }

        if (await CountAuctionsAsync(request.Auction.SellerCharacterId, buy: false, context.CancellationToken) >= 5)
        {
            return new AuctionRegisterResponse
            {
                Success = false,
                Auction = request.Auction
            };
        }

        var endTimeUnix = request.Auction.EndTimeUnix > 0
            ? request.Auction.EndTimeUnix
            : DateTimeOffset.UtcNow.AddHours(Math.Max(1, request.Auction.Hours)).ToUnixTimeSeconds();

        var auction = new AuctionEntity
        {
            SellerId = (int)request.Auction.SellerCharacterId,
            SellerName = Truncate(request.Auction.SellerName ?? string.Empty, 30),
            BuyerId = (int)request.Auction.BuyerCharacterId,
            BuyerName = Truncate(request.Auction.BuyerName ?? string.Empty, 30),
            NameId = SafeUIntFromInt(request.Auction.ItemId),
            ItemName = Truncate(request.Auction.ItemName ?? string.Empty, 50),
            Type = (short)Math.Clamp(request.Auction.ItemType, short.MinValue, short.MaxValue),
            Refine = (byte)Math.Clamp(request.Auction.Refine, 0, byte.MaxValue),
            Attribute = (byte)Math.Clamp(request.Auction.Attribute, 0, byte.MaxValue),
            Price = SafeUIntFromInt(request.Auction.Price),
            Buynow = SafeUIntFromInt(request.Auction.BuyNow),
            Hours = (short)Math.Clamp(request.Auction.Hours, short.MinValue, short.MaxValue),
            Timestamp = SafeUIntFromLong(endTimeUnix)
        };

        _dbContext.Auctions.Add(auction);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new AuctionRegisterResponse
        {
            Success = true,
            Auction = ToAuctionData(auction)
        };
    }

    public override async Task<AuctionCancelResponse> AuctionCancel(
        AuctionCancelRequest request,
        ServerCallContext context)
    {
        var auction = await _dbContext.Auctions
            .FirstOrDefaultAsync(a => a.AuctionId == request.AuctionId, context.CancellationToken);
        if (auction is null)
        {
            return new AuctionCancelResponse { Success = false, Result = 1 };
        }

        if (auction.SellerId != request.CharacterId)
        {
            return new AuctionCancelResponse { Success = false, Result = 2 };
        }

        if (auction.BuyerId > 0)
        {
            return new AuctionCancelResponse { Success = false, Result = 3 };
        }

        _dbContext.Auctions.Remove(auction);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new AuctionCancelResponse { Success = true, Result = 0 };
    }

    public override async Task<AuctionCloseResponse> AuctionClose(
        AuctionCloseRequest request,
        ServerCallContext context)
    {
        var auction = await _dbContext.Auctions
            .FirstOrDefaultAsync(a => a.AuctionId == request.AuctionId, context.CancellationToken);
        if (auction is null)
        {
            return new AuctionCloseResponse { Success = false, Result = 2 };
        }

        if (auction.SellerId != request.CharacterId || auction.BuyerId <= 0)
        {
            return new AuctionCloseResponse { Success = false, Result = 1 };
        }

        _dbContext.Auctions.Remove(auction);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new AuctionCloseResponse { Success = true, Result = 0 };
    }

    public override async Task<AuctionBidResponse> AuctionBid(
        AuctionBidRequest request,
        ServerCallContext context)
    {
        var auction = await _dbContext.Auctions
            .FirstOrDefaultAsync(a => a.AuctionId == request.AuctionId, context.CancellationToken);
        if (auction is null ||
            request.Bid <= auction.Price ||
            auction.SellerId == request.CharacterId)
        {
            return new AuctionBidResponse
            {
                Success = false,
                RefundZeny = request.Bid,
                Result = 0
            };
        }

        if (await CountAuctionsAsync(request.CharacterId, buy: true, context.CancellationToken) > 4 &&
            request.Bid < auction.Buynow &&
            auction.BuyerId != request.CharacterId)
        {
            return new AuctionBidResponse
            {
                Success = false,
                RefundZeny = request.Bid,
                Result = 9
            };
        }

        if (auction.BuyerId > 0)
        {
            var refundBody = auction.BuyerId == (int)request.CharacterId
                ? "You have placed a higher bid. The amount of zeny you offered has been returned to you."
                : "Someone has placed a higher bid. The amount of zeny you offered has been returned to you.";

            _dbContext.Mails.Add(new MailEntity
            {
                SendId = 0,
                SendName = "Auction Manager",
                DestId = auction.BuyerId,
                DestName = auction.BuyerName,
                Title = "Auction",
                Message = refundBody,
                Zeny = auction.Price,
                Time = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Status = 0,
                Type = 0
            });
        }

        auction.BuyerId = (int)request.CharacterId;
        auction.BuyerName = request.BidderName ?? string.Empty;
        auction.Price = SafeUIntFromInt(request.Bid);

        if (request.Bid >= auction.Buynow && auction.Buynow > 0)
        {
            var refund = request.Bid - (int)Math.Min(auction.Buynow, int.MaxValue);
            _dbContext.Auctions.Remove(auction);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            return new AuctionBidResponse
            {
                Success = true,
                RefundZeny = refund < 0 ? 0 : refund,
                Result = 1
            };
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new AuctionBidResponse
        {
            Success = true,
            RefundZeny = 0,
            Result = 1
        };
    }

    public override async Task<HotkeyLoadResponse> HotkeyLoad(
        HotkeyLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0) return new HotkeyLoadResponse { Success = false };

        var rows = await _dbContext.Hotkeys
            .AsNoTracking()
            .Where(h => h.CharId == request.CharacterId)
            .ToListAsync(context.CancellationToken);

        var response = new HotkeyLoadResponse { Success = true };
        foreach (var h in rows)
        {
            response.Hotkeys.Add(new HotkeyEntry
            {
                Hotkey = h.Hotkey,
                Type = h.Type,
                ItemSkillId = h.ItemSkillId,
                SkillLvl = h.SkillLvl,
            });
        }
        return response;
    }

    public override async Task<HotkeySaveResponse> HotkeySave(
        HotkeySaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0) return new HotkeySaveResponse { Success = false };
        var charId = (int)request.CharacterId;

        // rAthena mmo_char_tosql: DELETE all then INSERT non-empty rows.
        var existing = await _dbContext.Hotkeys
            .Where(h => h.CharId == charId)
            .ToListAsync(context.CancellationToken);
        if (existing.Count > 0) _dbContext.Hotkeys.RemoveRange(existing);

        foreach (var hk in request.Hotkeys)
        {
            // type == 0 means "empty slot"; skip those.
            if (hk.Type == 0) continue;
            _dbContext.Hotkeys.Add(new HotkeyEntity
            {
                CharId = charId,
                Hotkey = (byte)Math.Min(hk.Hotkey, byte.MaxValue),
                Type = (byte)Math.Min(hk.Type, byte.MaxValue),
                ItemSkillId = hk.ItemSkillId,
                SkillLvl = (byte)Math.Min(hk.SkillLvl, byte.MaxValue),
            });
        }
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new HotkeySaveResponse { Success = true };
    }

    public override async Task<QuestLoadResponse> QuestLoad(
        QuestLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new QuestLoadResponse { Success = false };
        }

        var response = new QuestLoadResponse { Success = true };
        var quests = await _dbContext.Quests
            .AsNoTracking()
            .Where(q => q.CharId == request.CharacterId)
            .ToListAsync(context.CancellationToken);
        response.Quests.AddRange(
            quests.OrderBy(entry => ParseQuestState(entry.State) == 2 ? 1 : 0)
                .Select(ToQuestEntryData));

        return response;
    }

    public override async Task<QuestSaveResponse> QuestSave(
        QuestSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new QuestSaveResponse { Success = false };
        }

        var charId = (int)request.CharacterId;
        var existing = await _dbContext.Quests
            .Where(q => q.CharId == charId)
            .ToListAsync(context.CancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.Quests.RemoveRange(existing);
        }

        foreach (var quest in request.Quests)
        {
            _dbContext.Quests.Add(ToQuestEntity(charId, quest));
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new QuestSaveResponse { Success = true };
    }

    public override async Task<AchievementLoadResponse> AchievementLoad(
        AchievementLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new AchievementLoadResponse { Success = false };
        }

        var response = new AchievementLoadResponse { Success = true };
        var achievements = await _dbContext.Achievements
            .AsNoTracking()
            .Where(a => a.CharId == request.CharacterId)
            .ToListAsync(context.CancellationToken);
        response.Achievements.AddRange(
            achievements.OrderBy(entry => entry.Completed.HasValue ? 1 : 0)
                .Select(ToAchievementEntryData));

        return response;
    }

    public override async Task<AchievementSaveResponse> AchievementSave(
        AchievementSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return new AchievementSaveResponse { Success = false };
        }

        var charId = (int)request.CharacterId;
        var existing = await _dbContext.Achievements
            .Where(a => a.CharId == charId)
            .ToListAsync(context.CancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.Achievements.RemoveRange(existing);
        }

        foreach (var achievement in request.Achievements)
        {
            _dbContext.Achievements.Add(ToAchievementEntity(charId, achievement));
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new AchievementSaveResponse { Success = true };
    }

    public override async Task<AchievementRewardResponse> AchievementReward(
        AchievementRewardRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0 || request.AchievementId <= 0)
        {
            return new AchievementRewardResponse { Success = false, RewardedUnix = 0 };
        }

        var achievement = await _dbContext.Achievements
            .FirstOrDefaultAsync(
                a => a.CharId == request.CharacterId && a.Id == request.AchievementId,
                context.CancellationToken);
        if (achievement is null || !achievement.Completed.HasValue || achievement.Rewarded.HasValue)
        {
            return new AchievementRewardResponse { Success = false, RewardedUnix = 0 };
        }

        achievement.Rewarded = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new AchievementRewardResponse
        {
            Success = true,
            RewardedUnix = ((DateTimeOffset)achievement.Rewarded.Value).ToUnixTimeSeconds()
        };
    }

    public override async Task<PetCreateResponse> PetCreate(
        PetCreateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.ClassId <= 0)
        {
            return new PetCreateResponse { Success = false, AccountId = request.AccountId };
        }

        var pet = new PetEntity
        {
            Class = SafeUIntFromInt(request.ClassId),
            Name = Truncate(request.Name ?? string.Empty, 24),
            AccountId = request.Incubate ? 0 : request.AccountId,
            CharId = request.Incubate ? 0 : request.CharacterId,
            Level = (ushort)Math.Clamp(request.Level, 0, ushort.MaxValue),
            EggId = SafeUIntFromInt(request.EggItemId),
            Equip = SafeUIntFromInt(request.EquipItemId),
            Intimate = (ushort)Math.Clamp(request.Intimacy, 0, ushort.MaxValue),
            Hungry = (ushort)Math.Clamp(request.Hungry, 0, ushort.MaxValue),
            RenameFlag = (byte)Math.Clamp(request.RenameFlag, 0, byte.MaxValue),
            Incubate = request.Incubate ? 1u : 0u
        };

        _dbContext.Pets.Add(pet);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new PetCreateResponse
        {
            Success = true,
            AccountId = request.AccountId,
            ClassId = (int)pet.Class,
            PetId = pet.PetId,
            Pet = ToPetData(pet)
        };
    }

    public override async Task<PetLoadResponse> PetLoad(
        PetLoadRequest request,
        ServerCallContext context)
    {
        var pet = await _dbContext.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PetId == request.PetId, context.CancellationToken);
        if (pet is null)
        {
            return new PetLoadResponse
            {
                Success = false,
                AccountId = request.AccountId,
                NoInfo = true
            };
        }

        var incubated = pet.Incubate != 0;
        var canLoad = incubated || (pet.AccountId == request.AccountId && pet.CharId == request.CharacterId);
        if (!canLoad)
        {
            return new PetLoadResponse
            {
                Success = false,
                AccountId = request.AccountId,
                NoInfo = true
            };
        }

        return new PetLoadResponse
        {
            Success = true,
            AccountId = request.AccountId,
            NoInfo = false,
            Pet = ToPetData(pet)
        };
    }

    public override async Task<PetSaveResponse> PetSave(
        PetSaveRequest request,
        ServerCallContext context)
    {
        if (request.Pet == null || request.Pet.PetId <= 0)
        {
            return new PetSaveResponse { Success = false, AccountId = request.AccountId };
        }

        var incoming = ToPetEntity(request.Pet);
        incoming.Hungry = incoming.Hungry > ushort.MaxValue ? ushort.MaxValue : incoming.Hungry;
        incoming.Intimate = incoming.Intimate > ushort.MaxValue ? ushort.MaxValue : incoming.Intimate;

        var existing = await _dbContext.Pets
            .FirstOrDefaultAsync(p => p.PetId == request.Pet.PetId, context.CancellationToken);
        if (existing is null)
        {
            _dbContext.Pets.Add(incoming);
        }
        else
        {
            existing.Class = incoming.Class;
            existing.Name = incoming.Name;
            existing.AccountId = incoming.AccountId;
            existing.CharId = incoming.CharId;
            existing.Level = incoming.Level;
            existing.EggId = incoming.EggId;
            existing.Equip = incoming.Equip;
            existing.Intimate = incoming.Intimate;
            existing.Hungry = incoming.Hungry;
            existing.RenameFlag = incoming.RenameFlag;
            existing.Incubate = incoming.Incubate;
            existing.Autofeed = incoming.Autofeed;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new PetSaveResponse
        {
            Success = true,
            AccountId = request.AccountId
        };
    }

    public override async Task<PetDeleteResponse> PetDelete(
        PetDeleteRequest request,
        ServerCallContext context)
    {
        var pet = await _dbContext.Pets
            .FirstOrDefaultAsync(p => p.PetId == request.PetId, context.CancellationToken);
        if (pet is not null)
        {
            _dbContext.Pets.Remove(pet);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }

        return new PetDeleteResponse { Success = true };
    }

    public override async Task<HomunculusCreateResponse> HomunculusCreate(
        HomunculusCreateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.Homunculus == null)
        {
            return new HomunculusCreateResponse { Success = false, AccountId = request.AccountId };
        }

        var entity = ToHomunculusEntity(request.Homunculus);
        entity.Intimacy = Math.Clamp(entity.Intimacy, 0, int.MaxValue);
        if (entity.Hunger < 0)
        {
            entity.Hunger = 0;
        }

        _dbContext.Homunculi.Add(entity);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await SaveHomunculusSkillsAsync(entity.HomunId, request.Homunculus.Skills, context.CancellationToken);

        return new HomunculusCreateResponse
        {
            Success = true,
            AccountId = request.AccountId,
            Homunculus = ToHomunculusData(entity, request.Homunculus.Skills)
        };
    }

    public override async Task<HomunculusLoadResponse> HomunculusLoad(
        HomunculusLoadRequest request,
        ServerCallContext context)
    {
        var entity = await _dbContext.Homunculi
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HomunId == request.HomunculusId, context.CancellationToken);
        if (entity is null)
        {
            return new HomunculusLoadResponse { Success = false, AccountId = request.AccountId };
        }

        var skills = await _dbContext.SkillHomunculi
            .AsNoTracking()
            .Where(s => s.HomunId == entity.HomunId)
            .ToListAsync(context.CancellationToken);

        return new HomunculusLoadResponse
        {
            Success = true,
            AccountId = request.AccountId,
            Homunculus = ToHomunculusData(entity, skills)
        };
    }

    public override async Task<HomunculusSaveResponse> HomunculusSave(
        HomunculusSaveRequest request,
        ServerCallContext context)
    {
        if (request.Homunculus == null || request.Homunculus.HomunculusId <= 0)
        {
            return new HomunculusSaveResponse { Success = false, AccountId = request.AccountId };
        }

        var incoming = ToHomunculusEntity(request.Homunculus);
        incoming.Intimacy = Math.Clamp(incoming.Intimacy, 0, int.MaxValue);
        if (incoming.Hunger < 0)
        {
            incoming.Hunger = 0;
        }

        var existing = await _dbContext.Homunculi
            .FirstOrDefaultAsync(h => h.HomunId == request.Homunculus.HomunculusId, context.CancellationToken);
        if (existing is null)
        {
            _dbContext.Homunculi.Add(incoming);
        }
        else
        {
            existing.CharId = incoming.CharId;
            existing.Class = incoming.Class;
            existing.PrevClass = incoming.PrevClass;
            existing.Name = incoming.Name;
            existing.Level = incoming.Level;
            existing.Exp = incoming.Exp;
            existing.Intimacy = incoming.Intimacy;
            existing.Hunger = incoming.Hunger;
            existing.Str = incoming.Str;
            existing.Agi = incoming.Agi;
            existing.Vit = incoming.Vit;
            existing.Int = incoming.Int;
            existing.Dex = incoming.Dex;
            existing.Luk = incoming.Luk;
            existing.Hp = incoming.Hp;
            existing.MaxHp = incoming.MaxHp;
            existing.Sp = incoming.Sp;
            existing.MaxSp = incoming.MaxSp;
            existing.SkillPoint = incoming.SkillPoint;
            existing.Alive = incoming.Alive;
            existing.RenameFlag = incoming.RenameFlag;
            existing.Vaporize = incoming.Vaporize;
            existing.Autofeed = incoming.Autofeed;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        await SaveHomunculusSkillsAsync(incoming.HomunId, request.Homunculus.Skills, context.CancellationToken);

        return new HomunculusSaveResponse { Success = true, AccountId = request.AccountId };
    }

    public override async Task<HomunculusDeleteResponse> HomunculusDelete(
        HomunculusDeleteRequest request,
        ServerCallContext context)
    {
        var entity = await _dbContext.Homunculi
            .FirstOrDefaultAsync(h => h.HomunId == request.HomunculusId, context.CancellationToken);
        if (entity is null)
        {
            return new HomunculusDeleteResponse { Success = false };
        }

        var skills = await _dbContext.SkillHomunculi
            .Where(s => s.HomunId == request.HomunculusId)
            .ToListAsync(context.CancellationToken);
        if (skills.Count > 0)
        {
            _dbContext.SkillHomunculi.RemoveRange(skills);
        }

        _dbContext.Homunculi.Remove(entity);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new HomunculusDeleteResponse { Success = true };
    }

    private async Task SaveHomunculusSkillsAsync(
        int homunId,
        Google.Protobuf.Collections.RepeatedField<HomunculusSkillEntry> skills,
        CancellationToken ct)
    {
        if (homunId <= 0)
        {
            return;
        }

        var existing = await _dbContext.SkillHomunculi
            .Where(s => s.HomunId == homunId)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            _dbContext.SkillHomunculi.RemoveRange(existing);
        }

        foreach (var skill in skills)
        {
            if (skill.Id <= 0 || skill.Lv == 0)
            {
                continue;
            }

            _dbContext.SkillHomunculi.Add(new SkillHomunculusEntity
            {
                HomunId = homunId,
                Id = skill.Id,
                Lv = (short)Math.Clamp(skill.Lv, short.MinValue, short.MaxValue)
            });
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public override Task<HomunculusRenameResponse> HomunculusRename(
        HomunculusRenameRequest request,
        ServerCallContext context)
    {
        var success = request.AccountId > 0 && request.CharacterId > 0 && !string.IsNullOrWhiteSpace(request.Name);
        return Task.FromResult(new HomunculusRenameResponse
        {
            Success = success,
            AccountId = request.AccountId,
            CharacterId = request.CharacterId,
            Name = request.Name ?? string.Empty
        });
    }

    public override async Task<MercenaryCreateResponse> MercenaryCreate(
        MercenaryCreateRequest request,
        ServerCallContext context)
    {
        if (request.Mercenary == null)
        {
            return new MercenaryCreateResponse { Success = false };
        }

        var entity = ToMercenaryEntity(request.Mercenary);
        _dbContext.Mercenaries.Add(entity);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new MercenaryCreateResponse
        {
            Success = true,
            Mercenary = ToMercenaryData(entity)
        };
    }

    public override async Task<MercenaryLoadResponse> MercenaryLoad(
        MercenaryLoadRequest request,
        ServerCallContext context)
    {
        var entity = await _dbContext.Mercenaries
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MerId == request.MercenaryId, context.CancellationToken);
        if (entity is null || entity.CharId != request.CharacterId)
        {
            return new MercenaryLoadResponse { Success = false };
        }

        return new MercenaryLoadResponse
        {
            Success = true,
            Mercenary = ToMercenaryData(entity)
        };
    }

    public override async Task<MercenarySaveResponse> MercenarySave(
        MercenarySaveRequest request,
        ServerCallContext context)
    {
        if (request.Mercenary == null || request.Mercenary.MercenaryId <= 0)
        {
            return new MercenarySaveResponse { Success = false };
        }

        var incoming = ToMercenaryEntity(request.Mercenary);
        var existing = await _dbContext.Mercenaries
            .FirstOrDefaultAsync(m => m.MerId == request.Mercenary.MercenaryId, context.CancellationToken);
        if (existing is null)
        {
            _dbContext.Mercenaries.Add(incoming);
        }
        else
        {
            existing.CharId = incoming.CharId;
            existing.Class = incoming.Class;
            existing.Hp = incoming.Hp;
            existing.Sp = incoming.Sp;
            existing.KillCounter = incoming.KillCounter;
            existing.LifeTime = incoming.LifeTime;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        // rAthena `mapif_mercenary_save` also persists skill cooldowns. Use the same
        // DELETE-all + INSERT-non-zero pattern used for homunculus skills.
        await SaveMercenarySkillCooldownsAsync(
            request.Mercenary.MercenaryId,
            request.Mercenary.Cooldowns,
            context.CancellationToken);

        return new MercenarySaveResponse { Success = true };
    }

    public override async Task<MercenaryDeleteResponse> MercenaryDelete(
        MercenaryDeleteRequest request,
        ServerCallContext context)
    {
        var entity = await _dbContext.Mercenaries
            .FirstOrDefaultAsync(m => m.MerId == request.MercenaryId, context.CancellationToken);
        if (entity is null)
        {
            return new MercenaryDeleteResponse { Success = false };
        }

        // rAthena `mercenary_owner_delete` cascades to skillcooldown_mercenary_db AND
        // mercenary_owner_db AND mercenary_db. Mirror the full cascade.
        var cooldowns = await _dbContext.SkillCooldownMercenaries
            .Where(s => s.MerId == request.MercenaryId)
            .ToListAsync(context.CancellationToken);
        if (cooldowns.Count > 0)
        {
            _dbContext.SkillCooldownMercenaries.RemoveRange(cooldowns);
        }

        var owners = await _dbContext.MercenaryOwners
            .Where(o => o.CharId == entity.CharId)
            .ToListAsync(context.CancellationToken);
        if (owners.Count > 0)
        {
            _dbContext.MercenaryOwners.RemoveRange(owners);
        }

        _dbContext.Mercenaries.Remove(entity);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new MercenaryDeleteResponse { Success = true };
    }

    private async Task SaveMercenarySkillCooldownsAsync(
        int merId,
        Google.Protobuf.Collections.RepeatedField<MercenarySkillCooldown> cooldowns,
        CancellationToken ct)
    {
        if (merId <= 0)
        {
            return;
        }

        var existing = await _dbContext.SkillCooldownMercenaries
            .Where(s => s.MerId == merId)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            _dbContext.SkillCooldownMercenaries.RemoveRange(existing);
        }

        foreach (var cd in cooldowns)
        {
            if (cd.Skill == 0)
            {
                continue;
            }
            _dbContext.SkillCooldownMercenaries.Add(new SkillCooldownMercenaryEntity
            {
                MerId = merId,
                Skill = (ushort)Math.Clamp(cd.Skill, 0u, ushort.MaxValue),
                Tick = cd.Tick
            });
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public override async Task<ElementalCreateResponse> ElementalCreate(
        ElementalCreateRequest request,
        ServerCallContext context)
    {
        if (request.Elemental == null)
        {
            return new ElementalCreateResponse { Success = false };
        }

        var entity = ToElementalEntity(request.Elemental);
        _dbContext.Elementals.Add(entity);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new ElementalCreateResponse
        {
            Success = true,
            Elemental = ToElementalData(entity)
        };
    }

    public override async Task<ElementalLoadResponse> ElementalLoad(
        ElementalLoadRequest request,
        ServerCallContext context)
    {
        var entity = await _dbContext.Elementals
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EleId == request.ElementalId, context.CancellationToken);
        if (entity is null || entity.CharId != request.CharacterId)
        {
            return new ElementalLoadResponse { Success = false };
        }

        return new ElementalLoadResponse
        {
            Success = true,
            Elemental = ToElementalData(entity)
        };
    }

    public override async Task<ElementalSaveResponse> ElementalSave(
        ElementalSaveRequest request,
        ServerCallContext context)
    {
        if (request.Elemental == null || request.Elemental.ElementalId <= 0)
        {
            return new ElementalSaveResponse { Success = false };
        }

        var incoming = ToElementalEntity(request.Elemental);
        var existing = await _dbContext.Elementals
            .FirstOrDefaultAsync(e => e.EleId == request.Elemental.ElementalId, context.CancellationToken);
        if (existing is null)
        {
            _dbContext.Elementals.Add(incoming);
        }
        else
        {
            existing.CharId = incoming.CharId;
            existing.Class = incoming.Class;
            existing.Mode = incoming.Mode;
            existing.Hp = incoming.Hp;
            existing.Sp = incoming.Sp;
            existing.MaxHp = incoming.MaxHp;
            existing.MaxSp = incoming.MaxSp;
            existing.Atk1 = incoming.Atk1;
            existing.Atk2 = incoming.Atk2;
            existing.Matk = incoming.Matk;
            existing.Aspd = incoming.Aspd;
            existing.Def = incoming.Def;
            existing.Mdef = incoming.Mdef;
            existing.Flee = incoming.Flee;
            existing.Hit = incoming.Hit;
            existing.LifeTime = incoming.LifeTime;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new ElementalSaveResponse { Success = true };
    }

    public override async Task<ElementalDeleteResponse> ElementalDelete(
        ElementalDeleteRequest request,
        ServerCallContext context)
    {
        var entity = await _dbContext.Elementals
            .FirstOrDefaultAsync(e => e.EleId == request.ElementalId, context.CancellationToken);
        if (entity is null)
        {
            return new ElementalDeleteResponse { Success = false };
        }

        _dbContext.Elementals.Remove(entity);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        return new ElementalDeleteResponse { Success = true };
    }

    public override async Task<ClanRequestResponse> ClanRequest(
        ClanRequestRequest request,
        ServerCallContext context)
    {
        var response = new ClanRequestResponse { Success = true };
        var clans = await _dbContext.Clans
            .AsNoTracking()
            .OrderBy(clan => clan.ClanId)
            .ToListAsync(context.CancellationToken);
        var alliances = await _dbContext.ClanAlliances
            .AsNoTracking()
            .ToListAsync(context.CancellationToken);
        var alliancesByClan = alliances
            .GroupBy(a => a.ClanId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.AllianceId).ToList());

        foreach (var clan in clans)
        {
            var data = new ClanData
            {
                ClanId = clan.ClanId,
                Name = clan.Name,
                Master = clan.Master,
                MapName = clan.MapName,
                MaxMember = clan.MaxMember,
                ConnectMember = clan.ConnectMember
            };

            if (alliancesByClan.TryGetValue(clan.ClanId, out var clanAlliances))
            {
                data.Alliances.AddRange(clanAlliances.Select(a => new ClanAllianceData
                {
                    Opposition = a.Opposition != 0,
                    ClanId = a.AllianceId,
                    Name = a.Name
                }));
            }

            response.Clans.Add(data);
        }

        return response;
    }

    public override async Task<ClanMessageResponse> ClanMessage(
        ClanMessageRequest request,
        ServerCallContext context)
    {
        var clanExists = request.ClanId > 0 &&
                         await _dbContext.Clans
                             .AsNoTracking()
                             .AnyAsync(c => c.ClanId == request.ClanId, context.CancellationToken);
        var success = clanExists && !string.IsNullOrWhiteSpace(request.Message);
        return new ClanMessageResponse { Success = success };
    }

    public override async Task<ClanMemberStateResponse> ClanMemberLeft(
        ClanMemberStateRequest request,
        ServerCallContext context)
    {
        var clan = await _dbContext.Clans
            .FirstOrDefaultAsync(c => c.ClanId == request.ClanId, context.CancellationToken);
        if (clan is null)
        {
            return new ClanMemberStateResponse
            {
                Success = false,
                ClanId = request.ClanId,
                ConnectMember = 0
            };
        }

        if (clan.ConnectMember > 0)
        {
            clan.ConnectMember--;
        }
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new ClanMemberStateResponse
        {
            Success = true,
            ClanId = request.ClanId,
            ConnectMember = clan.ConnectMember
        };
    }

    public override async Task<ClanMemberStateResponse> ClanMemberJoined(
        ClanMemberStateRequest request,
        ServerCallContext context)
    {
        var clan = await _dbContext.Clans
            .FirstOrDefaultAsync(c => c.ClanId == request.ClanId, context.CancellationToken);
        if (clan is null)
        {
            return new ClanMemberStateResponse
            {
                Success = false,
                ClanId = request.ClanId,
                ConnectMember = 0
            };
        }

        if (clan.ConnectMember < ushort.MaxValue)
        {
            clan.ConnectMember++;
        }
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new ClanMemberStateResponse
        {
            Success = true,
            ClanId = request.ClanId,
            ConnectMember = clan.ConnectMember
        };
    }

    private static MailMessageData ToMailMessageData(MailEntity mail, IReadOnlyDictionary<int, int> accountsByCharId)
        => ToMailMessageData(mail, accountsByCharId, Array.Empty<MailAttachmentEntity>());

    private static MailMessageData ToMailMessageData(
        MailEntity mail,
        IReadOnlyDictionary<int, int> accountsByCharId,
        IEnumerable<MailAttachmentEntity> attachments)
    {
        var data = new MailMessageData
        {
            MailId = mail.Id,
            SenderAccountId = accountsByCharId.GetValueOrDefault(mail.SendId, 0),
            SenderCharacterId = mail.SendId,
            SenderName = mail.SendName,
            ReceiverAccountId = accountsByCharId.GetValueOrDefault(mail.DestId, 0),
            ReceiverCharacterId = mail.DestId,
            ReceiverName = mail.DestName,
            Title = mail.Title,
            Body = mail.Message,
            Zeny = mail.Zeny,
            Attachment = Google.Protobuf.ByteString.Empty,
            Opened = mail.Status != 0
        };

        foreach (var att in attachments)
        {
            data.Items.Add(ToMailAttachmentItem(att));
        }
        return data;
    }

    private static MailAttachmentEntity ToMailAttachmentEntity(long mailId, MailAttachmentItem item)
    {
        return new MailAttachmentEntity
        {
            Id = mailId,
            Index = (ushort)Math.Clamp(item.Index, 0u, ushort.MaxValue),
            NameId = item.NameId,
            Amount = item.Amount,
            Refine = (byte)Math.Clamp(item.Refine, 0u, byte.MaxValue),
            Attribute = (byte)Math.Clamp(item.Attribute, 0u, byte.MaxValue),
            Identify = (short)Math.Clamp(item.Identify, short.MinValue, short.MaxValue),
            Card0 = item.Card0,
            Card1 = item.Card1,
            Card2 = item.Card2,
            Card3 = item.Card3,
            OptionId0 = (short)Math.Clamp(item.OptionId0, short.MinValue, short.MaxValue),
            OptionVal0 = (short)Math.Clamp(item.OptionVal0, short.MinValue, short.MaxValue),
            OptionParm0 = (sbyte)Math.Clamp(item.OptionParm0, sbyte.MinValue, sbyte.MaxValue),
            OptionId1 = (short)Math.Clamp(item.OptionId1, short.MinValue, short.MaxValue),
            OptionVal1 = (short)Math.Clamp(item.OptionVal1, short.MinValue, short.MaxValue),
            OptionParm1 = (sbyte)Math.Clamp(item.OptionParm1, sbyte.MinValue, sbyte.MaxValue),
            OptionId2 = (short)Math.Clamp(item.OptionId2, short.MinValue, short.MaxValue),
            OptionVal2 = (short)Math.Clamp(item.OptionVal2, short.MinValue, short.MaxValue),
            OptionParm2 = (sbyte)Math.Clamp(item.OptionParm2, sbyte.MinValue, sbyte.MaxValue),
            OptionId3 = (short)Math.Clamp(item.OptionId3, short.MinValue, short.MaxValue),
            OptionVal3 = (short)Math.Clamp(item.OptionVal3, short.MinValue, short.MaxValue),
            OptionParm3 = (sbyte)Math.Clamp(item.OptionParm3, sbyte.MinValue, sbyte.MaxValue),
            OptionId4 = (short)Math.Clamp(item.OptionId4, short.MinValue, short.MaxValue),
            OptionVal4 = (short)Math.Clamp(item.OptionVal4, short.MinValue, short.MaxValue),
            OptionParm4 = (sbyte)Math.Clamp(item.OptionParm4, sbyte.MinValue, sbyte.MaxValue),
            UniqueId = item.UniqueId,
            Bound = (byte)Math.Clamp(item.Bound, 0u, byte.MaxValue),
            EnchantGrade = (byte)Math.Clamp(item.EnchantGrade, 0u, byte.MaxValue)
        };
    }

    private static MailAttachmentItem ToMailAttachmentItem(MailAttachmentEntity att)
    {
        return new MailAttachmentItem
        {
            Index = att.Index,
            NameId = att.NameId,
            Amount = att.Amount,
            Refine = att.Refine,
            Attribute = att.Attribute,
            Identify = att.Identify,
            Card0 = att.Card0,
            Card1 = att.Card1,
            Card2 = att.Card2,
            Card3 = att.Card3,
            OptionId0 = att.OptionId0,
            OptionVal0 = att.OptionVal0,
            OptionParm0 = att.OptionParm0,
            OptionId1 = att.OptionId1,
            OptionVal1 = att.OptionVal1,
            OptionParm1 = att.OptionParm1,
            OptionId2 = att.OptionId2,
            OptionVal2 = att.OptionVal2,
            OptionParm2 = att.OptionParm2,
            OptionId3 = att.OptionId3,
            OptionVal3 = att.OptionVal3,
            OptionParm3 = att.OptionParm3,
            OptionId4 = att.OptionId4,
            OptionVal4 = att.OptionVal4,
            OptionParm4 = att.OptionParm4,
            UniqueId = att.UniqueId,
            Bound = att.Bound,
            EnchantGrade = att.EnchantGrade
        };
    }

    private static AuctionData ToAuctionData(AuctionEntity auction)
    {
        return new AuctionData
        {
            AuctionId = auction.AuctionId,
            SellerCharacterId = auction.SellerId,
            SellerName = auction.SellerName,
            BuyerCharacterId = auction.BuyerId,
            BuyerName = auction.BuyerName,
            ItemId = (int)Math.Min(auction.NameId, int.MaxValue),
            ItemName = auction.ItemName,
            ItemType = auction.Type,
            Refine = auction.Refine,
            Attribute = auction.Attribute,
            Price = (int)Math.Min(auction.Price, int.MaxValue),
            BuyNow = (int)Math.Min(auction.Buynow, int.MaxValue),
            Hours = auction.Hours,
            EndTimeUnix = auction.Timestamp,
            ItemPayload = Google.Protobuf.ByteString.Empty
        };
    }

    private static bool MatchesAuctionRequest(AuctionEntity auction, AuctionRequestListRequest request)
    {
        return request.Type switch
        {
            4 => string.IsNullOrWhiteSpace(request.SearchText) ||
                 auction.ItemName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase),
            5 => auction.Price <= request.Price,
            6 => auction.SellerId == request.CharacterId,
            7 => auction.BuyerId == request.CharacterId,
            _ => true
        };
    }

    private async Task<int> CountAuctionsAsync(long characterId, bool buy, CancellationToken ct)
    {
        if (characterId <= 0)
        {
            return 0;
        }

        var id = (int)Math.Min(characterId, int.MaxValue);
        return buy
            ? await _dbContext.Auctions.AsNoTracking().CountAsync(auction => auction.BuyerId == id, ct)
            : await _dbContext.Auctions.AsNoTracking().CountAsync(auction => auction.SellerId == id, ct);
    }

    private static QuestEntryData ToQuestEntryData(QuestEntity quest)
    {
        var data = new QuestEntryData
        {
            QuestId = (int)Math.Min(quest.QuestId, int.MaxValue),
            TimeUnix = quest.Time,
            State = ParseQuestState(quest.State)
        };
        data.Counts.Add((int)quest.Count1);
        data.Counts.Add((int)quest.Count2);
        data.Counts.Add((int)quest.Count3);
        return data;
    }

    private static QuestEntity ToQuestEntity(int charId, QuestEntryData quest)
    {
        return new QuestEntity
        {
            CharId = charId,
            QuestId = SafeUIntFromInt(quest.QuestId),
            Time = SafeUIntFromLong(quest.TimeUnix),
            State = quest.State.ToString(),
            Count1 = SafeUIntFromRepeated(quest.Counts, 0),
            Count2 = SafeUIntFromRepeated(quest.Counts, 1),
            Count3 = SafeUIntFromRepeated(quest.Counts, 2)
        };
    }

    private static AchievementEntryData ToAchievementEntryData(AchievementEntity achievement)
    {
        var data = new AchievementEntryData
        {
            AchievementId = (int)Math.Clamp(achievement.Id, int.MinValue, int.MaxValue),
            CompletedUnix = achievement.Completed.HasValue ? ((DateTimeOffset)achievement.Completed.Value).ToUnixTimeSeconds() : 0,
            RewardedUnix = achievement.Rewarded.HasValue ? ((DateTimeOffset)achievement.Rewarded.Value).ToUnixTimeSeconds() : 0,
            Score = 0
        };
        data.Counts.Add((int)achievement.Count1);
        data.Counts.Add((int)achievement.Count2);
        data.Counts.Add((int)achievement.Count3);
        data.Counts.Add((int)achievement.Count4);
        data.Counts.Add((int)achievement.Count5);
        data.Counts.Add((int)achievement.Count6);
        data.Counts.Add((int)achievement.Count7);
        data.Counts.Add((int)achievement.Count8);
        data.Counts.Add((int)achievement.Count9);
        data.Counts.Add((int)achievement.Count10);
        return data;
    }

    private static AchievementEntity ToAchievementEntity(int charId, AchievementEntryData achievement)
    {
        return new AchievementEntity
        {
            CharId = charId,
            Id = achievement.AchievementId,
            Count1 = SafeUIntFromRepeated(achievement.Counts, 0),
            Count2 = SafeUIntFromRepeated(achievement.Counts, 1),
            Count3 = SafeUIntFromRepeated(achievement.Counts, 2),
            Count4 = SafeUIntFromRepeated(achievement.Counts, 3),
            Count5 = SafeUIntFromRepeated(achievement.Counts, 4),
            Count6 = SafeUIntFromRepeated(achievement.Counts, 5),
            Count7 = SafeUIntFromRepeated(achievement.Counts, 6),
            Count8 = SafeUIntFromRepeated(achievement.Counts, 7),
            Count9 = SafeUIntFromRepeated(achievement.Counts, 8),
            Count10 = SafeUIntFromRepeated(achievement.Counts, 9),
            Completed = achievement.CompletedUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(achievement.CompletedUnix).UtcDateTime
                : null,
            Rewarded = achievement.RewardedUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(achievement.RewardedUnix).UtcDateTime
                : null
        };
    }

    private static uint SafeUIntFromRepeated(Google.Protobuf.Collections.RepeatedField<int> values, int index)
    {
        if (index < 0 || index >= values.Count)
        {
            return 0;
        }
        return values[index] < 0 ? 0u : (uint)values[index];
    }

    private static uint SafeUIntFromLong(long value)
        => value < 0 ? 0u : value > uint.MaxValue ? uint.MaxValue : (uint)value;

    private static uint SafeUIntFromInt(int value)
        => value < 0 ? 0u : (uint)value;

    private static int ParseQuestState(string? state)
        => int.TryParse(state, out var parsed) ? parsed : 0;

    private static PetData ToPetData(PetEntity pet)
    {
        return new PetData
        {
            PetId = pet.PetId,
            AccountId = pet.AccountId,
            CharacterId = pet.CharId,
            ClassId = (int)pet.Class,
            Level = pet.Level,
            EggItemId = (int)Math.Min(pet.EggId, int.MaxValue),
            EquipItemId = (int)Math.Min(pet.Equip, int.MaxValue),
            Intimacy = pet.Intimate,
            Hungry = pet.Hungry,
            RenameFlag = pet.RenameFlag,
            Incubate = pet.Incubate != 0,
            Name = pet.Name,
            Payload = Google.Protobuf.ByteString.Empty
        };
    }

    private static PetEntity ToPetEntity(PetData pet)
    {
        return new PetEntity
        {
            PetId = pet.PetId,
            Class = SafeUIntFromInt(pet.ClassId),
            Name = Truncate(pet.Name, 24),
            AccountId = pet.AccountId,
            CharId = pet.CharacterId,
            Level = (ushort)Math.Clamp(pet.Level, 0, ushort.MaxValue),
            EggId = SafeUIntFromInt(pet.EggItemId),
            Equip = SafeUIntFromInt(pet.EquipItemId),
            Intimate = (ushort)Math.Clamp(pet.Intimacy, 0, ushort.MaxValue),
            Hungry = (ushort)Math.Clamp(pet.Hungry, 0, ushort.MaxValue),
            RenameFlag = (byte)Math.Clamp(pet.RenameFlag, 0, byte.MaxValue),
            Incubate = pet.Incubate ? 1u : 0u
        };
    }

    private static HomunculusData ToHomunculusData(HomunculusEntity hom)
        => ToHomunculusData(hom, Array.Empty<SkillHomunculusEntity>());

    private static HomunculusData ToHomunculusData(HomunculusEntity hom, IEnumerable<SkillHomunculusEntity> skills)
    {
        var data = new HomunculusData
        {
            HomunculusId = hom.HomunId,
            CharacterId = hom.CharId,
            ClassId = (int)hom.Class,
            Name = hom.Name,
            Level = hom.Level,
            Exp = (long)Math.Min(hom.Exp, (ulong)long.MaxValue),
            Intimacy = hom.Intimacy,
            Hunger = hom.Hunger,
            Hp = (int)Math.Min(hom.Hp, int.MaxValue),
            MaxHp = (int)Math.Min(hom.MaxHp, int.MaxValue),
            Sp = (int)Math.Min(hom.Sp, int.MaxValue),
            MaxSp = (int)Math.Min(hom.MaxSp, int.MaxValue),
            Payload = Google.Protobuf.ByteString.Empty
        };

        foreach (var skill in skills)
        {
            data.Skills.Add(new HomunculusSkillEntry { Id = skill.Id, Lv = skill.Lv });
        }

        return data;
    }

    private static HomunculusData ToHomunculusData(HomunculusEntity hom, Google.Protobuf.Collections.RepeatedField<HomunculusSkillEntry> skills)
    {
        var data = ToHomunculusData(hom);
        foreach (var skill in skills)
        {
            data.Skills.Add(new HomunculusSkillEntry { Id = skill.Id, Lv = skill.Lv });
        }
        return data;
    }

    private static HomunculusEntity ToHomunculusEntity(HomunculusData hom)
    {
        return new HomunculusEntity
        {
            HomunId = hom.HomunculusId,
            CharId = hom.CharacterId,
            Class = SafeUIntFromInt(hom.ClassId),
            Name = Truncate(hom.Name, 24),
            Level = (short)Math.Clamp(hom.Level, short.MinValue, short.MaxValue),
            Exp = hom.Exp < 0 ? 0ul : (ulong)hom.Exp,
            Intimacy = Math.Max(hom.Intimacy, 0),
            Hunger = (short)Math.Clamp(hom.Hunger, short.MinValue, short.MaxValue),
            Hp = SafeUIntFromInt(hom.Hp),
            MaxHp = SafeUIntFromInt(hom.MaxHp),
            Sp = SafeUIntFromInt(hom.Sp),
            MaxSp = SafeUIntFromInt(hom.MaxSp),
            Alive = 1
        };
    }

    private static MercenaryData ToMercenaryData(MercenaryEntity merc)
    {
        return new MercenaryData
        {
            MercenaryId = merc.MerId,
            CharacterId = merc.CharId,
            ClassId = (int)merc.Class,
            Hp = (int)Math.Min(merc.Hp, int.MaxValue),
            Sp = (int)Math.Min(merc.Sp, int.MaxValue),
            KillCount = merc.KillCounter,
            LifeTime = merc.LifeTime,
            Payload = Google.Protobuf.ByteString.Empty
        };
    }

    private static MercenaryEntity ToMercenaryEntity(MercenaryData merc)
    {
        return new MercenaryEntity
        {
            MerId = merc.MercenaryId,
            CharId = merc.CharacterId,
            Class = SafeUIntFromInt(merc.ClassId),
            Hp = SafeUIntFromInt(merc.Hp),
            Sp = SafeUIntFromInt(merc.Sp),
            KillCounter = merc.KillCount,
            LifeTime = merc.LifeTime
        };
    }

    private static ElementalData ToElementalData(ElementalEntity ele)
    {
        return new ElementalData
        {
            ElementalId = ele.EleId,
            CharacterId = ele.CharId,
            ClassId = (int)ele.Class,
            Mode = (int)Math.Min(ele.Mode, int.MaxValue),
            Hp = (int)Math.Min(ele.Hp, int.MaxValue),
            Sp = (int)Math.Min(ele.Sp, int.MaxValue),
            MaxHp = (int)Math.Min(ele.MaxHp, int.MaxValue),
            MaxSp = (int)Math.Min(ele.MaxSp, int.MaxValue),
            Attack = (int)Math.Min(ele.Atk1, int.MaxValue),
            Attack2 = (int)Math.Min(ele.Atk2, int.MaxValue),
            Matk = (int)Math.Min(ele.Matk, int.MaxValue),
            Aspd = ele.Aspd,
            Def = ele.Def,
            Mdef = ele.Mdef,
            Flee = ele.Flee,
            Hit = ele.Hit,
            LifeTime = ele.LifeTime,
            Payload = Google.Protobuf.ByteString.Empty
        };
    }

    private static ElementalEntity ToElementalEntity(ElementalData ele)
    {
        return new ElementalEntity
        {
            EleId = ele.ElementalId,
            CharId = ele.CharacterId,
            Class = SafeUIntFromInt(ele.ClassId),
            Mode = SafeUIntFromInt(ele.Mode),
            Hp = SafeUIntFromInt(ele.Hp),
            Sp = SafeUIntFromInt(ele.Sp),
            MaxHp = SafeUIntFromInt(ele.MaxHp),
            MaxSp = SafeUIntFromInt(ele.MaxSp),
            Atk1 = SafeUIntFromInt(ele.Attack),
            Atk2 = SafeUIntFromInt(ele.Attack2),
            Matk = SafeUIntFromInt(ele.Matk),
            Aspd = (ushort)Math.Clamp(ele.Aspd, 0, ushort.MaxValue),
            Def = (ushort)Math.Clamp(ele.Def, 0, ushort.MaxValue),
            Mdef = (ushort)Math.Clamp(ele.Mdef, 0, ushort.MaxValue),
            Flee = (ushort)Math.Clamp(ele.Flee, 0, ushort.MaxValue),
            Hit = (ushort)Math.Clamp(ele.Hit, 0, ushort.MaxValue),
            LifeTime = ele.LifeTime
        };
    }

    private async Task<PartyInfoData?> LoadPartyInfoDataAsync(int partyId, CancellationToken ct)
    {
        var party = await _dbContext.Parties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PartyId == partyId, ct);
        if (party is null)
        {
            return null;
        }

        var members = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.PartyId == partyId && c.DeleteDate == 0)
            .OrderBy(c => c.CharId == party.LeaderChar ? 0 : 1)
            .ThenBy(c => c.CharId)
            .ToListAsync(ct);

        var data = new PartyInfoData
        {
            PartyId = party.PartyId,
            Name = party.Name,
            Item = party.Item,
            // Current schema has a single `party.item`; keep item2 aligned until schema expands.
            Item2 = party.Item,
            LeaderCharacterId = party.LeaderChar
        };

        data.Members.AddRange(members.Select(member => new PartyMemberInfo
        {
            AccountId = member.AccountId,
            CharacterId = member.CharId,
            Name = member.Name,
            ClassId = member.Class,
            MapName = member.LastMap,
            Level = member.BaseLevel,
            Online = member.Online != 0
        }));

        return data;
    }

    private static int GetGuildCastleIndexValue(GuildCastleEntity? castle, int index)
    {
        if (castle is null)
        {
            return 0;
        }

        return index switch
        {
            1 => castle.GuildId,
            2 => SafeInt(castle.Economy),
            3 => SafeInt(castle.Defense),
            4 => SafeInt(castle.TriggerE),
            5 => SafeInt(castle.TriggerD),
            6 => SafeInt(castle.NextTime),
            7 => SafeInt(castle.PayTime),
            8 => SafeInt(castle.CreateTime),
            9 => SafeInt(castle.VisibleC),
            10 => SafeInt(castle.VisibleG0),
            11 => SafeInt(castle.VisibleG1),
            12 => SafeInt(castle.VisibleG2),
            13 => SafeInt(castle.VisibleG3),
            14 => SafeInt(castle.VisibleG4),
            15 => SafeInt(castle.VisibleG5),
            16 => SafeInt(castle.VisibleG6),
            17 => SafeInt(castle.VisibleG7),
            _ => 0
        };
    }

    private static bool TrySetGuildCastleIndexValue(GuildCastleEntity castle, int index, int value)
    {
        switch (index)
        {
            case 1: castle.GuildId = value; return true;
            case 2: castle.Economy = SafeUInt(value); return true;
            case 3: castle.Defense = SafeUInt(value); return true;
            case 4: castle.TriggerE = SafeUInt(value); return true;
            case 5: castle.TriggerD = SafeUInt(value); return true;
            case 6: castle.NextTime = SafeUInt(value); return true;
            case 7: castle.PayTime = SafeUInt(value); return true;
            case 8: castle.CreateTime = SafeUInt(value); return true;
            case 9: castle.VisibleC = SafeUInt(value); return true;
            case 10: castle.VisibleG0 = SafeUInt(value); return true;
            case 11: castle.VisibleG1 = SafeUInt(value); return true;
            case 12: castle.VisibleG2 = SafeUInt(value); return true;
            case 13: castle.VisibleG3 = SafeUInt(value); return true;
            case 14: castle.VisibleG4 = SafeUInt(value); return true;
            case 15: castle.VisibleG5 = SafeUInt(value); return true;
            case 16: castle.VisibleG6 = SafeUInt(value); return true;
            case 17: castle.VisibleG7 = SafeUInt(value); return true;
            default:
                return false;
        }
    }

    private static uint SafeUInt(int value) => value < 0 ? 0u : (uint)value;
    private static int SafeInt(uint value) => value > int.MaxValue ? int.MaxValue : (int)value;

    private async Task<GuildInfoData?> LoadGuildInfoDataAsync(int guildId, CancellationToken ct)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GuildId == guildId, ct);
        if (guild is null)
        {
            return null;
        }

        var members = await _dbContext.GuildMembers
            .AsNoTracking()
            .Where(m => m.GuildId == guildId)
            .OrderBy(m => m.Position)
            .ThenBy(m => m.CharId)
            .ToListAsync(ct);

        var positions = await _dbContext.GuildPositions
            .AsNoTracking()
            .Where(p => p.GuildId == guildId)
            .OrderBy(p => p.Position)
            .ToListAsync(ct);
        var positionById = positions.ToDictionary(p => p.Position, p => p);

        var memberCharIds = members.Select(m => m.CharId).ToList();
        var memberChars = memberCharIds.Count == 0
            ? new List<CharEntity>()
            : await _dbContext.Characters
                .AsNoTracking()
                .Where(c => memberCharIds.Contains(c.CharId))
                .ToListAsync(ct);
        var memberCharById = memberChars.ToDictionary(c => c.CharId, c => c);

        var data = new GuildInfoData
        {
            GuildId = guild.GuildId,
            Name = guild.Name,
            Level = guild.GuildLv,
            MaxMember = guild.MaxMember,
            MasterCharacterId = guild.CharId,
            EmblemVersion = (int)Math.Min(guild.EmblemId, int.MaxValue),
            EmblemData = Google.Protobuf.ByteString.CopyFrom(guild.EmblemData ?? Array.Empty<byte>()),
            Notice1 = guild.Mes1,
            Notice2 = guild.Mes2
        };

        data.Members.AddRange(members.Select(member =>
        {
            memberCharById.TryGetValue(member.CharId, out var character);
            var positionName = positionById.TryGetValue(member.Position, out var pos)
                ? pos.Name
                : member.Position == 0 ? "Master" : "Member";

            return new GuildMemberInfo
            {
                AccountId = character?.AccountId ?? 0,
                CharacterId = member.CharId,
                Name = character?.Name ?? string.Empty,
                ClassId = character?.Class ?? 0,
                Level = character?.BaseLevel ?? 0,
                Online = (character?.Online ?? 0) != 0,
                PositionName = positionName
            };
        }));

        data.Positions.AddRange(positions.Select(position => new GuildPositionInfo
        {
            Index = position.Position,
            Name = position.Name,
            Mode = position.Mode,
            ExpMode = position.ExpMode
        }));

        return data;
    }

    private async Task UpsertGuildAllianceAsync(
        int guildId,
        int allianceId,
        int opposition,
        string allianceName,
        CancellationToken ct)
    {
        var entry = await _dbContext.GuildAlliances
            .FirstOrDefaultAsync(a => a.GuildId == guildId && a.AllianceId == allianceId, ct);
        if (entry is null)
        {
            _dbContext.GuildAlliances.Add(new GuildAllianceEntity
            {
                GuildId = guildId,
                AllianceId = allianceId,
                Opposition = opposition,
                Name = allianceName
            });
            return;
        }

        entry.Opposition = opposition;
        entry.Name = allianceName;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }
        return value[..maxLength];
    }

    private async Task<CharEntity?> LoadCharacterEntityAsync(long characterId, CancellationToken cancellationToken)
    {
        if (characterId <= 0 || characterId > int.MaxValue)
        {
            return null;
        }

        return await _characterRepository.GetByIdAsync((int)characterId, cancellationToken);
    }

    private static CharacterInfo ToCharacterInfo(CharEntity character)
    {
        return new CharacterInfo
        {
            CharacterId = character.CharId,
            Name = character.Name,
            Level = character.BaseLevel,
            ClassId = character.Class,
            CreatedAt = character.LastLogin.HasValue
                ? new DateTimeOffset(character.LastLogin.Value).ToUnixTimeSeconds()
                : 0
        };
    }

    // rAthena stores sex as char ('M'/'F'/'S'/'U'); the wire protocol uses
    // 0 = female, 1 = male (everything non-M maps to 0).
    private static uint SexCharToByte(string sex)
    {
        return string.Equals(sex, "M", StringComparison.OrdinalIgnoreCase) ? 1u : 0u;
    }

    private static CharacterDataResponse ToCharacterDataResponse(CharEntity character)
    {
        return new CharacterDataResponse
        {
            Character = ToCharacterInfo(character),
            MapId = 0,
            MapName = character.LastMap ?? string.Empty,
            PositionX = character.LastX,
            PositionY = character.LastY,
            PositionZ = 0,
            // Status fields needed by the map server's initial-status
            // broadcast cascade. See proto comment + initial-status-broadcast.md.
            Str = character.Str,
            Agi = character.Agi,
            Vit = character.Vit,
            IntStat = character.Int,
            Dex = character.Dex,
            Luk = character.Luk,
            StatusPoint = character.StatusPoint,
            SkillPoint = character.SkillPoint,
            BaseLevel = character.BaseLevel,
            JobLevel = character.JobLevel,
            BaseExp = character.BaseExp,
            JobExp = character.JobExp,
            Hp = character.Hp,
            MaxHp = character.MaxHp,
            Sp = character.Sp,
            MaxSp = character.MaxSp,
            Zeny = character.Zeny,
            ClassId = character.Class,
            Weapon = character.Weapon,
            Shield = character.Shield,
            Pow = character.Pow,
            Sta = character.Sta,
            Wis = character.Wis,
            Spl = character.Spl,
            Con = character.Con,
            Crt = character.Crt,
            TraitPoint = character.TraitPoint,
            Ap = character.Ap,
            MaxAp = character.MaxAp,
        };
    }

    private static bool TryFindAvailableSlot(IReadOnlyCollection<CharEntity> activeCharacters, out byte slot)
    {
        var usedSlots = activeCharacters.Select(c => c.CharNum).ToHashSet();
        for (byte i = 0; i < 9; i++)
        {
            if (!usedSlots.Contains(i))
            {
                slot = i;
                return true;
            }
        }

        slot = 0;
        return false;
    }

    private static bool IsDeleteBlockedByBaseLevel(ushort baseLevel, int deleteLevelConfig)
    {
        if (deleteLevelConfig == 0)
        {
            return false;
        }

        if (deleteLevelConfig > 0)
        {
            return baseLevel >= deleteLevelConfig;
        }

        return baseLevel <= -deleteLevelConfig;
    }

    private static byte[] SerializeStatusChangeRows(IReadOnlyCollection<ScDataEntity> rows)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)1); // payload version
        writer.Write(rows.Count);
        foreach (var row in rows)
        {
            writer.Write(row.AccountId);
            writer.Write(row.CharId);
            writer.Write(row.Type);
            writer.Write(row.Tick);
            writer.Write(row.Val1);
            writer.Write(row.Val2);
            writer.Write(row.Val3);
            writer.Write(row.Val4);
        }

        return ms.ToArray();
    }

    private static bool TryDeserializeStatusChangeRows(byte[] payload, out List<ScDataEntity> rows)
    {
        rows = [];
        try
        {
            using var ms = new MemoryStream(payload);
            using var reader = new BinaryReader(ms);
            var version = reader.ReadByte();
            if (version != 1)
            {
                return false;
            }

            var count = reader.ReadInt32();
            if (count < 0)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                rows.Add(new ScDataEntity
                {
                    AccountId = reader.ReadInt32(),
                    CharId = reader.ReadInt32(),
                    Type = reader.ReadUInt16(),
                    Tick = reader.ReadInt64(),
                    Val1 = reader.ReadInt32(),
                    Val2 = reader.ReadInt32(),
                    Val3 = reader.ReadInt32(),
                    Val4 = reader.ReadInt32()
                });
            }

            return ms.Position == ms.Length;
        }
        catch
        {
            rows = [];
            return false;
        }
    }

    private static byte[] SerializeSkillCooldownRows(IReadOnlyCollection<SkillCooldownEntity> rows)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)1); // payload version
        writer.Write(rows.Count);
        foreach (var row in rows)
        {
            writer.Write(row.AccountId);
            writer.Write(row.CharId);
            writer.Write(row.Skill);
            writer.Write(row.Tick);
        }

        return ms.ToArray();
    }

    private static bool TryDeserializeSkillCooldownRows(byte[] payload, out List<SkillCooldownEntity> rows)
    {
        rows = [];
        try
        {
            using var ms = new MemoryStream(payload);
            using var reader = new BinaryReader(ms);
            var version = reader.ReadByte();
            if (version != 1)
            {
                return false;
            }

            var count = reader.ReadInt32();
            if (count < 0)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                rows.Add(new SkillCooldownEntity
                {
                    AccountId = reader.ReadInt32(),
                    CharId = reader.ReadInt32(),
                    Skill = reader.ReadUInt16(),
                    Tick = reader.ReadInt64()
                });
            }

            return ms.Position == ms.Length;
        }
        catch
        {
            rows = [];
            return false;
        }
    }

    private static byte[] SerializeBonusScriptRows(IReadOnlyCollection<BonusScriptEntity> rows)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)1); // payload version
        writer.Write(rows.Count);
        foreach (var row in rows)
        {
            var scriptBytes = Encoding.UTF8.GetBytes(row.Script ?? string.Empty);
            writer.Write(scriptBytes.Length);
            writer.Write(scriptBytes);
            writer.Write(row.Tick);
            writer.Write(row.Flag);
            writer.Write(row.Type);
            writer.Write(row.Icon);
        }

        return ms.ToArray();
    }

    private static bool TryDeserializeBonusScriptRows(byte[] payload, out List<BonusScriptEntity> rows)
    {
        rows = [];
        try
        {
            using var ms = new MemoryStream(payload);
            using var reader = new BinaryReader(ms);
            var version = reader.ReadByte();
            if (version != 1)
            {
                return false;
            }

            var count = reader.ReadInt32();
            if (count < 0)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var length = reader.ReadInt32();
                if (length < 0)
                {
                    return false;
                }

                var script = Encoding.UTF8.GetString(reader.ReadBytes(length));
                rows.Add(new BonusScriptEntity
                {
                    Script = script,
                    Tick = reader.ReadInt64(),
                    Flag = reader.ReadUInt16(),
                    Type = reader.ReadByte(),
                    Icon = reader.ReadInt16()
                });
            }

            return ms.Position == ms.Length;
        }
        catch
        {
            rows = [];
            return false;
        }
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
        // Kick the char-server session list locally...
        var disconnected = await _charServer.ForceDisconnectAccountAsync(request.AccountId);
        // ...and cascade to every connected map server so any in-game session is dropped.
        var mapDisconnected = await _mapServerIpc.ForceDisconnectAccountOnMapsAsync(
            request.AccountId, context.CancellationToken);
        return new ForceDisconnectAccountResponse
        {
            Success = true,
            DisconnectedSessions = disconnected + mapDisconnected
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

    public override async Task<AddressSyncResponse> RequestAddressSync(
        AddressSyncRequest request,
        ServerCallContext context)
    {
        // rAthena 0x2735 parity: trigger char-side IP re-resolution + re-push to login,
        // and fan out 0x2b1e to all connected map servers so they re-resolve their own.
        _charServer.TriggerAddressSync();
        await _mapServerIpc.NotifyAddressSyncAsync(context.CancellationToken);
        return new AddressSyncResponse { Success = true };
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
