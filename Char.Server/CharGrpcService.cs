using Char.Server.Services;
using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server;
using Grpc.Core;
using System.Collections.Concurrent;
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
    private readonly ILoginServerIpcService _loginServerIpc;
    private readonly ICharacterRepository _characterRepository;
    private readonly IFriendRepository _friendRepository;
    private readonly GameDbContext _dbContext;
    private readonly CharServerConfiguration _configuration;
    private readonly ILogger<CharGrpcService> _logger;
    private readonly ConcurrentDictionary<int, string[]> _mapServerMaps = new();
    private readonly ConcurrentDictionary<int, int> _mapServerUserCounts = new();
    private readonly ConcurrentDictionary<int, (uint Ip, uint Port)> _mapServerAddresses = new();
    private readonly ConcurrentDictionary<long, byte[]> _statusChangeDataByCharacter = new();
    private readonly ConcurrentDictionary<long, byte[]> _skillCooldownByCharacter = new();
    private readonly ConcurrentDictionary<long, byte[]> _bonusScriptByCharacter = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<long, int>> _fameByType = new();
    private uint _partyShareLevel = 0;
    private readonly ConcurrentDictionary<int, byte[]> _guildStorageByGuild = new();
    private readonly ConcurrentDictionary<int, byte[]> _accountStorageByAccount = new();
    private readonly ConcurrentDictionary<long, MailState> _mailById = new();
    private readonly ConcurrentDictionary<long, List<long>> _mailByReceiverCharacter = new();
    private int _nextMailId = 5000;
    private readonly ConcurrentDictionary<long, AuctionState> _auctionsById = new();
    private int _nextAuctionId = 7000;
    private readonly ConcurrentDictionary<int, PetState> _petsById = new();
    private readonly ConcurrentDictionary<int, MercenaryState> _mercenariesById = new();
    private readonly ConcurrentDictionary<int, ElementalState> _elementalsById = new();
    private readonly ConcurrentDictionary<int, HomunculusState> _homunculiById = new();
    private readonly ConcurrentDictionary<int, ClanState> _clansById = new();
    private int _nextPetId = 9000;
    private int _nextMercenaryId = 11000;
    private int _nextElementalId = 12000;
    private int _nextHomunculusId = 13000;

    public CharGrpcService(
        CharServerImpl charServer,
        IMapAuthTicketService mapAuthTicketService,
        ILoginServerIpcService loginServerIpc,
        ICharacterRepository characterRepository,
        IFriendRepository friendRepository,
        GameDbContext dbContext,
        CharServerConfiguration configuration,
        ILogger<CharGrpcService> logger)
    {
        _charServer = charServer;
        _mapAuthTicketService = mapAuthTicketService;
        _loginServerIpc = loginServerIpc;
        _characterRepository = characterRepository;
        _friendRepository = friendRepository;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _clansById.TryAdd(1, new ClanState
        {
            ClanId = 1,
            Name = "Swordsman Clan",
            Master = "Valhalla",
            MapName = "prontera",
            MaxMember = 500,
            ConnectMember = 0
        });
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
                CharacterData = ToCharacterDataResponse(autotradeCharacter)
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

        return new CharacterMapAuthResponse
        {
            Success = true,
            AccountId = request.AccountId,
            CharacterId = request.CharacterId,
            LoginId1 = request.LoginId1,
            LoginId2 = request.LoginId2,
            ExpirationTime = 0,
            GroupId = 0,
            ChangingMapServers = true,
            CharacterData = characterData
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

        var maps = request.MapNames
            .Where(map => !string.IsNullOrWhiteSpace(map))
            .Select(map => map.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _mapServerMaps[request.MapServerId] = maps;

        return Task.FromResult(new MapServerMapRegistryResponse
        {
            Success = true,
            RegisteredMaps = maps.Length
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

        _mapServerUserCounts.TryGetValue(request.MapServerId, out var users);
        return Task.FromResult(new MapServerUserCountResponse { Success = true, UserCount = users });
    }

    public override Task<MapServerUserCountUpdateResponse> RegisterMapServerUserCount(
        MapServerUserCountUpdateRequest request,
        ServerCallContext context)
    {
        if (request.MapServerId <= 0 || request.UserCount < 0)
        {
            return Task.FromResult(new MapServerUserCountUpdateResponse { Success = false });
        }

        _mapServerUserCounts[request.MapServerId] = request.UserCount;
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

        _mapServerAddresses[request.MapServerId] = (request.Ip, request.Port);
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
        var updatedAny = false;
        foreach (var character in characters.Where(c => c.DeleteDate == 0 &&
                                                        (!request.CharacterId.Equals(0) ? c.CharId == request.CharacterId : true)))
        {
            if (character.Online == 0)
            {
                continue;
            }

            character.Online = 0;
            await _characterRepository.UpdateAsync(character, context.CancellationToken);
            updatedAny = true;
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

        if (!updatedAny && request.CharacterId > 0)
        {
            return new CharacterOnlineStateResponse { Success = false };
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

        // Keep current in-memory cache in sync during migration; DB is source of truth.
        var fame = _fameByType.GetOrAdd(request.FameType, _ => new ConcurrentDictionary<long, int>());
        fame[request.CharacterId] = request.Value;

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

    public override Task<InterBroadcastResponse> InterBroadcast(
        InterBroadcastRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Inter broadcast from account {AccountId}, char {CharacterId}: {Message}",
            request.SourceAccountId,
            request.SourceCharacterId,
            request.Message);
        return Task.FromResult(new InterBroadcastResponse { Success = true });
    }

    public override Task<InterBroadcastItemResponse> InterBroadcastItem(
        InterBroadcastItemRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Inter item broadcast from account {AccountId}, char {CharacterId}: item={ItemId} amount={Amount}",
            request.SourceAccountId,
            request.SourceCharacterId,
            request.ItemId,
            request.Amount);
        return Task.FromResult(new InterBroadcastItemResponse { Success = true });
    }

    public override Task<InterWhisperResponse> InterWhisper(
        InterWhisperRequest request,
        ServerCallContext context)
    {
        var ok = !string.IsNullOrWhiteSpace(request.SourceName)
            && !string.IsNullOrWhiteSpace(request.TargetName)
            && !string.IsNullOrWhiteSpace(request.Message);
        return Task.FromResult(new InterWhisperResponse
        {
            Success = ok,
            ErrorMessage = ok ? string.Empty : "Invalid whisper request"
        });
    }

    public override Task<InterWhisperReplyResponse> InterWhisperReply(
        InterWhisperReplyRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new InterWhisperReplyResponse { Success = true });
    }

    public override Task<InterWhisperToGmResponse> InterWhisperToGm(
        InterWhisperToGmRequest request,
        ServerCallContext context)
    {
        var ok = !string.IsNullOrWhiteSpace(request.SourceName) && !string.IsNullOrWhiteSpace(request.Message);
        return Task.FromResult(new InterWhisperToGmResponse { Success = ok });
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

    public override Task<InterNameChangeResponse> InterNameChange(
        InterNameChangeRequest request,
        ServerCallContext context)
    {
        var ok = request.CharacterId > 0 && !string.IsNullOrWhiteSpace(request.NewName);
        return Task.FromResult(new InterNameChangeResponse
        {
            Success = ok,
            ErrorMessage = ok ? string.Empty : "Invalid name change request"
        });
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

        character.PartyId = 0;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        var remainingMembers = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.PartyId == request.PartyId && c.DeleteDate == 0)
            .OrderBy(c => c.CharId)
            .Select(c => new { c.CharId, c.AccountId })
            .ToListAsync(context.CancellationToken);

        if (remainingMembers.Count == 0)
        {
            _dbContext.Parties.Remove(party);
        }
        else if (party.LeaderChar == request.CharacterId)
        {
            party.LeaderChar = remainingMembers[0].CharId;
            party.LeaderId = remainingMembers[0].AccountId;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
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
        _partyShareLevel = request.ShareLevel;
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

        var members = await _dbContext.Characters
            .Where(c => c.GuildId == request.GuildId)
            .ToListAsync(context.CancellationToken);
        foreach (var member in members)
        {
            member.GuildId = 0;
        }

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

    public override Task<GuildStorageLoadResponse> GuildStorageLoad(
        GuildStorageLoadRequest request,
        ServerCallContext context)
    {
        if (request.GuildId <= 0)
        {
            return Task.FromResult(new GuildStorageLoadResponse { Success = false });
        }

        _guildStorageByGuild.TryGetValue(request.GuildId, out var data);
        return Task.FromResult(new GuildStorageLoadResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        });
    }

    public override Task<GuildStorageSaveResponse> GuildStorageSave(
        GuildStorageSaveRequest request,
        ServerCallContext context)
    {
        if (request.GuildId <= 0)
        {
            return Task.FromResult(new GuildStorageSaveResponse { Success = false });
        }

        _guildStorageByGuild[request.GuildId] = request.Data.ToByteArray();
        return Task.FromResult(new GuildStorageSaveResponse { Success = true });
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

    public override Task<AccountStorageLoadResponse> AccountStorageLoad(
        AccountStorageLoadRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return Task.FromResult(new AccountStorageLoadResponse { Success = false });
        }

        _accountStorageByAccount.TryGetValue(request.AccountId, out var data);
        return Task.FromResult(new AccountStorageLoadResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        });
    }

    public override Task<AccountStorageSaveResponse> AccountStorageSave(
        AccountStorageSaveRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return Task.FromResult(new AccountStorageSaveResponse { Success = false });
        }

        _accountStorageByAccount[request.AccountId] = request.Data.ToByteArray();
        return Task.FromResult(new AccountStorageSaveResponse { Success = true });
    }

    public override Task<MailRequestInboxResponse> MailRequestInbox(
        MailRequestInboxRequest request,
        ServerCallContext context)
    {
        var response = new MailRequestInboxResponse { Success = request.CharacterId > 0 };
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(response);
        }

        if (_mailByReceiverCharacter.TryGetValue(request.CharacterId, out var mailIds))
        {
            lock (mailIds)
            {
                foreach (var mailId in mailIds)
                {
                    if (_mailById.TryGetValue(mailId, out var mail))
                    {
                        response.Mails.Add(ToMailMessageData(mail));
                    }
                }
            }
        }

        return Task.FromResult(response);
    }

    public override Task<MailReadResponse> MailRead(
        MailReadRequest request,
        ServerCallContext context)
    {
        if (!_mailById.TryGetValue(request.MailId, out var mail) || mail.ReceiverCharacterId != request.CharacterId)
        {
            return Task.FromResult(new MailReadResponse { Success = false });
        }

        mail.Opened = true;
        return Task.FromResult(new MailReadResponse
        {
            Success = true,
            Mail = ToMailMessageData(mail)
        });
    }

    public override Task<MailGetAttachmentResponse> MailGetAttachment(
        MailGetAttachmentRequest request,
        ServerCallContext context)
    {
        if (!_mailById.TryGetValue(request.MailId, out var mail) || mail.ReceiverCharacterId != request.CharacterId)
        {
            return Task.FromResult(new MailGetAttachmentResponse { Success = false });
        }

        var zeny = mail.Zeny;
        var attachment = mail.Attachment;
        mail.Zeny = 0;
        mail.Attachment = Array.Empty<byte>();

        return Task.FromResult(new MailGetAttachmentResponse
        {
            Success = true,
            Zeny = zeny,
            Attachment = Google.Protobuf.ByteString.CopyFrom(attachment)
        });
    }

    public override Task<MailDeleteResponse> MailDelete(
        MailDeleteRequest request,
        ServerCallContext context)
    {
        var success = _mailById.TryRemove(request.MailId, out var removed);
        if (success && removed != null && _mailByReceiverCharacter.TryGetValue(removed.ReceiverCharacterId, out var list))
        {
            lock (list)
            {
                list.Remove(request.MailId);
            }
        }

        return Task.FromResult(new MailDeleteResponse { Success = success });
    }

    public override Task<MailReturnResponse> MailReturn(
        MailReturnRequest request,
        ServerCallContext context)
    {
        if (!_mailById.TryGetValue(request.MailId, out var mail) || mail.ReceiverCharacterId != request.CharacterId)
        {
            return Task.FromResult(new MailReturnResponse { Success = false });
        }

        var returnedId = Interlocked.Increment(ref _nextMailId);
        var returned = new MailState
        {
            MailId = returnedId,
            SenderAccountId = mail.ReceiverAccountId,
            SenderCharacterId = mail.ReceiverCharacterId,
            SenderName = mail.ReceiverName,
            ReceiverAccountId = mail.SenderAccountId,
            ReceiverCharacterId = mail.SenderCharacterId,
            ReceiverName = mail.SenderName,
            Title = $"RE: {mail.Title}",
            Body = mail.Body,
            Zeny = mail.Zeny,
            Attachment = mail.Attachment.ToArray(),
            Opened = false
        };

        _mailById[returnedId] = returned;
        var inbox = _mailByReceiverCharacter.GetOrAdd(returned.ReceiverCharacterId, _ => new List<long>());
        lock (inbox)
        {
            inbox.Add(returnedId);
        }

        _mailById.TryRemove(request.MailId, out _);
        if (_mailByReceiverCharacter.TryGetValue(mail.ReceiverCharacterId, out var currentInbox))
        {
            lock (currentInbox)
            {
                currentInbox.Remove(request.MailId);
            }
        }

        return Task.FromResult(new MailReturnResponse { Success = true });
    }

    public override Task<MailSendResponse> MailSend(
        MailSendRequest request,
        ServerCallContext context)
    {
        if (request.ReceiverCharacterId <= 0 || string.IsNullOrWhiteSpace(request.ReceiverName))
        {
            return Task.FromResult(new MailSendResponse { Success = false });
        }

        var mailId = Interlocked.Increment(ref _nextMailId);
        var mail = new MailState
        {
            MailId = mailId,
            SenderAccountId = request.SenderAccountId,
            SenderCharacterId = request.SenderCharacterId,
            SenderName = request.SenderName ?? string.Empty,
            ReceiverAccountId = request.ReceiverAccountId,
            ReceiverCharacterId = request.ReceiverCharacterId,
            ReceiverName = request.ReceiverName ?? string.Empty,
            Title = request.Title ?? string.Empty,
            Body = request.Body ?? string.Empty,
            Zeny = request.Zeny,
            Attachment = request.Attachment.ToByteArray(),
            Opened = false
        };

        _mailById[mailId] = mail;
        var inbox = _mailByReceiverCharacter.GetOrAdd(mail.ReceiverCharacterId, _ => new List<long>());
        lock (inbox)
        {
            inbox.Add(mailId);
        }

        return Task.FromResult(new MailSendResponse
        {
            Success = true,
            MailId = mailId
        });
    }

    public override Task<MailReceiverCheckResponse> MailReceiverCheck(
        MailReceiverCheckRequest request,
        ServerCallContext context)
    {
        var ok = !string.IsNullOrWhiteSpace(request.ReceiverName);
        return Task.FromResult(new MailReceiverCheckResponse
        {
            Success = ok,
            AccountId = 0,
            CharacterId = 0,
            ReceiverName = request.ReceiverName ?? string.Empty
        });
    }

    public override Task<AuctionRequestListResponse> AuctionRequestList(
        AuctionRequestListRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new AuctionRequestListResponse { Success = false, Count = 0, Pages = 0 });
        }

        var requestedPage = Math.Max(1, request.Page);
        var filtered = _auctionsById.Values
            .Where(auction => MatchesAuctionRequest(auction, request))
            .OrderByDescending(auction => auction.AuctionId)
            .ToList();

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
        return Task.FromResult(response);
    }

    public override Task<AuctionRegisterResponse> AuctionRegister(
        AuctionRegisterRequest request,
        ServerCallContext context)
    {
        if (request.Auction == null || request.Auction.SellerCharacterId <= 0)
        {
            return Task.FromResult(new AuctionRegisterResponse { Success = false });
        }

        if (CountAuctions(request.Auction.SellerCharacterId, buy: false) >= 5)
        {
            return Task.FromResult(new AuctionRegisterResponse
            {
                Success = false,
                Auction = request.Auction
            });
        }

        var auctionId = Interlocked.Increment(ref _nextAuctionId);
        var endTimeUnix = request.Auction.EndTimeUnix > 0
            ? request.Auction.EndTimeUnix
            : DateTimeOffset.UtcNow.AddHours(Math.Max(1, request.Auction.Hours)).ToUnixTimeSeconds();

        var auction = new AuctionState
        {
            AuctionId = auctionId,
            SellerCharacterId = request.Auction.SellerCharacterId,
            SellerName = request.Auction.SellerName ?? string.Empty,
            BuyerCharacterId = request.Auction.BuyerCharacterId,
            BuyerName = request.Auction.BuyerName ?? string.Empty,
            ItemId = request.Auction.ItemId,
            ItemName = request.Auction.ItemName ?? string.Empty,
            ItemType = request.Auction.ItemType,
            Refine = request.Auction.Refine,
            Attribute = request.Auction.Attribute,
            Price = request.Auction.Price,
            BuyNow = request.Auction.BuyNow,
            Hours = request.Auction.Hours,
            EndTimeUnix = endTimeUnix,
            ItemPayload = request.Auction.ItemPayload.ToByteArray()
        };

        _auctionsById[auctionId] = auction;
        return Task.FromResult(new AuctionRegisterResponse
        {
            Success = true,
            Auction = ToAuctionData(auction)
        });
    }

    public override Task<AuctionCancelResponse> AuctionCancel(
        AuctionCancelRequest request,
        ServerCallContext context)
    {
        if (!_auctionsById.TryGetValue(request.AuctionId, out var auction))
        {
            return Task.FromResult(new AuctionCancelResponse { Success = false, Result = 1 });
        }

        if (auction.SellerCharacterId != request.CharacterId)
        {
            return Task.FromResult(new AuctionCancelResponse { Success = false, Result = 2 });
        }

        if (auction.BuyerCharacterId > 0)
        {
            return Task.FromResult(new AuctionCancelResponse { Success = false, Result = 3 });
        }

        _auctionsById.TryRemove(request.AuctionId, out _);
        return Task.FromResult(new AuctionCancelResponse { Success = true, Result = 0 });
    }

    public override Task<AuctionCloseResponse> AuctionClose(
        AuctionCloseRequest request,
        ServerCallContext context)
    {
        if (!_auctionsById.TryGetValue(request.AuctionId, out var auction))
        {
            return Task.FromResult(new AuctionCloseResponse { Success = false, Result = 2 });
        }

        if (auction.SellerCharacterId != request.CharacterId || auction.BuyerCharacterId <= 0)
        {
            return Task.FromResult(new AuctionCloseResponse { Success = false, Result = 1 });
        }

        _auctionsById.TryRemove(request.AuctionId, out _);
        return Task.FromResult(new AuctionCloseResponse { Success = true, Result = 0 });
    }

    public override Task<AuctionBidResponse> AuctionBid(
        AuctionBidRequest request,
        ServerCallContext context)
    {
        if (!_auctionsById.TryGetValue(request.AuctionId, out var auction) ||
            request.Bid <= auction.Price ||
            auction.SellerCharacterId == request.CharacterId)
        {
            return Task.FromResult(new AuctionBidResponse
            {
                Success = false,
                RefundZeny = request.Bid,
                Result = 0
            });
        }

        if (CountAuctions(request.CharacterId, buy: true) > 4 &&
            request.Bid < auction.BuyNow &&
            auction.BuyerCharacterId != request.CharacterId)
        {
            return Task.FromResult(new AuctionBidResponse
            {
                Success = false,
                RefundZeny = request.Bid,
                Result = 9
            });
        }

        auction.BuyerCharacterId = request.CharacterId;
        auction.BuyerName = request.BidderName ?? string.Empty;
        auction.Price = request.Bid;

        if (request.Bid >= auction.BuyNow && auction.BuyNow > 0)
        {
            _auctionsById.TryRemove(request.AuctionId, out _);
            return Task.FromResult(new AuctionBidResponse
            {
                Success = true,
                RefundZeny = request.Bid - auction.BuyNow,
                Result = 1
            });
        }

        return Task.FromResult(new AuctionBidResponse
        {
            Success = true,
            RefundZeny = 0,
            Result = 1
        });
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

    public override Task<PetCreateResponse> PetCreate(
        PetCreateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.ClassId <= 0)
        {
            return Task.FromResult(new PetCreateResponse { Success = false, AccountId = request.AccountId });
        }

        var petId = Interlocked.Increment(ref _nextPetId);
        var pet = new PetState
        {
            PetId = petId,
            AccountId = request.Incubate ? 0 : request.AccountId,
            CharacterId = request.Incubate ? 0 : request.CharacterId,
            ClassId = request.ClassId,
            Level = request.Level,
            EggItemId = request.EggItemId,
            EquipItemId = request.EquipItemId,
            Intimacy = Math.Clamp(request.Intimacy, 0, 1000),
            Hungry = Math.Clamp(request.Hungry, 0, 100),
            RenameFlag = request.RenameFlag,
            Incubate = request.Incubate,
            Name = request.Name ?? string.Empty
        };

        _petsById[petId] = pet;
        return Task.FromResult(new PetCreateResponse
        {
            Success = true,
            AccountId = request.AccountId,
            ClassId = pet.ClassId,
            PetId = pet.PetId,
            Pet = ToPetData(pet)
        });
    }

    public override Task<PetLoadResponse> PetLoad(
        PetLoadRequest request,
        ServerCallContext context)
    {
        if (!_petsById.TryGetValue(request.PetId, out var pet))
        {
            return Task.FromResult(new PetLoadResponse
            {
                Success = false,
                AccountId = request.AccountId,
                NoInfo = true
            });
        }

        var canLoad = pet.Incubate || (pet.AccountId == request.AccountId && pet.CharacterId == request.CharacterId);
        if (!canLoad)
        {
            return Task.FromResult(new PetLoadResponse
            {
                Success = false,
                AccountId = request.AccountId,
                NoInfo = true
            });
        }

        if (pet.Incubate)
        {
            pet.AccountId = 0;
            pet.CharacterId = 0;
        }

        return Task.FromResult(new PetLoadResponse
        {
            Success = true,
            AccountId = request.AccountId,
            NoInfo = false,
            Pet = ToPetData(pet)
        });
    }

    public override Task<PetSaveResponse> PetSave(
        PetSaveRequest request,
        ServerCallContext context)
    {
        if (request.Pet == null || request.Pet.PetId <= 0)
        {
            return Task.FromResult(new PetSaveResponse { Success = false, AccountId = request.AccountId });
        }

        var pet = ToPetState(request.Pet);
        pet.Hungry = Math.Clamp(pet.Hungry, 0, 100);
        pet.Intimacy = Math.Clamp(pet.Intimacy, 0, 1000);
        _petsById[pet.PetId] = pet;

        return Task.FromResult(new PetSaveResponse
        {
            Success = true,
            AccountId = request.AccountId
        });
    }

    public override Task<PetDeleteResponse> PetDelete(
        PetDeleteRequest request,
        ServerCallContext context)
    {
        _petsById.TryRemove(request.PetId, out _);
        return Task.FromResult(new PetDeleteResponse { Success = true });
    }

    public override Task<HomunculusCreateResponse> HomunculusCreate(
        HomunculusCreateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.Homunculus == null)
        {
            return Task.FromResult(new HomunculusCreateResponse { Success = false, AccountId = request.AccountId });
        }

        var state = ToHomunculusState(request.Homunculus);
        if (state.HomunculusId <= 0)
        {
            state.HomunculusId = Interlocked.Increment(ref _nextHomunculusId);
        }

        state.Intimacy = Math.Clamp(state.Intimacy, 0, 100000);
        state.Hunger = Math.Clamp(state.Hunger, 0, 100);
        _homunculiById[state.HomunculusId] = state;

        return Task.FromResult(new HomunculusCreateResponse
        {
            Success = true,
            AccountId = request.AccountId,
            Homunculus = ToHomunculusData(state)
        });
    }

    public override Task<HomunculusLoadResponse> HomunculusLoad(
        HomunculusLoadRequest request,
        ServerCallContext context)
    {
        if (!_homunculiById.TryGetValue(request.HomunculusId, out var state))
        {
            return Task.FromResult(new HomunculusLoadResponse { Success = false, AccountId = request.AccountId });
        }

        return Task.FromResult(new HomunculusLoadResponse
        {
            Success = true,
            AccountId = request.AccountId,
            Homunculus = ToHomunculusData(state)
        });
    }

    public override Task<HomunculusSaveResponse> HomunculusSave(
        HomunculusSaveRequest request,
        ServerCallContext context)
    {
        if (request.Homunculus == null || request.Homunculus.HomunculusId <= 0)
        {
            return Task.FromResult(new HomunculusSaveResponse { Success = false, AccountId = request.AccountId });
        }

        var state = ToHomunculusState(request.Homunculus);
        state.Intimacy = Math.Clamp(state.Intimacy, 0, 100000);
        state.Hunger = Math.Clamp(state.Hunger, 0, 100);
        _homunculiById[state.HomunculusId] = state;
        return Task.FromResult(new HomunculusSaveResponse { Success = true, AccountId = request.AccountId });
    }

    public override Task<HomunculusDeleteResponse> HomunculusDelete(
        HomunculusDeleteRequest request,
        ServerCallContext context)
    {
        var success = _homunculiById.TryRemove(request.HomunculusId, out _);
        return Task.FromResult(new HomunculusDeleteResponse { Success = success });
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

    public override Task<MercenaryCreateResponse> MercenaryCreate(
        MercenaryCreateRequest request,
        ServerCallContext context)
    {
        if (request.Mercenary == null)
        {
            return Task.FromResult(new MercenaryCreateResponse { Success = false });
        }

        var state = ToMercenaryState(request.Mercenary);
        if (state.MercenaryId <= 0)
        {
            state.MercenaryId = Interlocked.Increment(ref _nextMercenaryId);
        }

        _mercenariesById[state.MercenaryId] = state;
        return Task.FromResult(new MercenaryCreateResponse
        {
            Success = true,
            Mercenary = ToMercenaryData(state)
        });
    }

    public override Task<MercenaryLoadResponse> MercenaryLoad(
        MercenaryLoadRequest request,
        ServerCallContext context)
    {
        if (!_mercenariesById.TryGetValue(request.MercenaryId, out var state) || state.CharacterId != request.CharacterId)
        {
            return Task.FromResult(new MercenaryLoadResponse { Success = false });
        }

        return Task.FromResult(new MercenaryLoadResponse
        {
            Success = true,
            Mercenary = ToMercenaryData(state)
        });
    }

    public override Task<MercenarySaveResponse> MercenarySave(
        MercenarySaveRequest request,
        ServerCallContext context)
    {
        if (request.Mercenary == null || request.Mercenary.MercenaryId <= 0)
        {
            return Task.FromResult(new MercenarySaveResponse { Success = false });
        }

        var state = ToMercenaryState(request.Mercenary);
        _mercenariesById[state.MercenaryId] = state;
        return Task.FromResult(new MercenarySaveResponse { Success = true });
    }

    public override Task<MercenaryDeleteResponse> MercenaryDelete(
        MercenaryDeleteRequest request,
        ServerCallContext context)
    {
        var success = _mercenariesById.TryRemove(request.MercenaryId, out _);
        return Task.FromResult(new MercenaryDeleteResponse { Success = success });
    }

    public override Task<ElementalCreateResponse> ElementalCreate(
        ElementalCreateRequest request,
        ServerCallContext context)
    {
        if (request.Elemental == null)
        {
            return Task.FromResult(new ElementalCreateResponse { Success = false });
        }

        var state = ToElementalState(request.Elemental);
        if (state.ElementalId <= 0)
        {
            state.ElementalId = Interlocked.Increment(ref _nextElementalId);
        }

        _elementalsById[state.ElementalId] = state;
        return Task.FromResult(new ElementalCreateResponse
        {
            Success = true,
            Elemental = ToElementalData(state)
        });
    }

    public override Task<ElementalLoadResponse> ElementalLoad(
        ElementalLoadRequest request,
        ServerCallContext context)
    {
        if (!_elementalsById.TryGetValue(request.ElementalId, out var state) || state.CharacterId != request.CharacterId)
        {
            return Task.FromResult(new ElementalLoadResponse { Success = false });
        }

        return Task.FromResult(new ElementalLoadResponse
        {
            Success = true,
            Elemental = ToElementalData(state)
        });
    }

    public override Task<ElementalSaveResponse> ElementalSave(
        ElementalSaveRequest request,
        ServerCallContext context)
    {
        if (request.Elemental == null || request.Elemental.ElementalId <= 0)
        {
            return Task.FromResult(new ElementalSaveResponse { Success = false });
        }

        var state = ToElementalState(request.Elemental);
        _elementalsById[state.ElementalId] = state;
        return Task.FromResult(new ElementalSaveResponse { Success = true });
    }

    public override Task<ElementalDeleteResponse> ElementalDelete(
        ElementalDeleteRequest request,
        ServerCallContext context)
    {
        var success = _elementalsById.TryRemove(request.ElementalId, out _);
        return Task.FromResult(new ElementalDeleteResponse { Success = success });
    }

    public override Task<ClanRequestResponse> ClanRequest(
        ClanRequestRequest request,
        ServerCallContext context)
    {
        var response = new ClanRequestResponse { Success = true };
        response.Clans.AddRange(_clansById.Values
            .OrderBy(clan => clan.ClanId)
            .Select(ToClanData));
        return Task.FromResult(response);
    }

    public override Task<ClanMessageResponse> ClanMessage(
        ClanMessageRequest request,
        ServerCallContext context)
    {
        var success = request.ClanId > 0 &&
            _clansById.ContainsKey(request.ClanId) &&
            !string.IsNullOrWhiteSpace(request.Message);
        return Task.FromResult(new ClanMessageResponse { Success = success });
    }

    public override Task<ClanMemberStateResponse> ClanMemberLeft(
        ClanMemberStateRequest request,
        ServerCallContext context)
    {
        if (!_clansById.TryGetValue(request.ClanId, out var clan))
        {
            return Task.FromResult(new ClanMemberStateResponse
            {
                Success = false,
                ClanId = request.ClanId,
                ConnectMember = 0
            });
        }

        lock (clan.SyncRoot)
        {
            if (clan.ConnectMember > 0)
            {
                clan.ConnectMember--;
            }
        }

        return Task.FromResult(new ClanMemberStateResponse
        {
            Success = true,
            ClanId = clan.ClanId,
            ConnectMember = clan.ConnectMember
        });
    }

    public override Task<ClanMemberStateResponse> ClanMemberJoined(
        ClanMemberStateRequest request,
        ServerCallContext context)
    {
        if (!_clansById.TryGetValue(request.ClanId, out var clan))
        {
            return Task.FromResult(new ClanMemberStateResponse
            {
                Success = false,
                ClanId = request.ClanId,
                ConnectMember = 0
            });
        }

        lock (clan.SyncRoot)
        {
            clan.ConnectMember++;
        }

        return Task.FromResult(new ClanMemberStateResponse
        {
            Success = true,
            ClanId = clan.ClanId,
            ConnectMember = clan.ConnectMember
        });
    }

    private static MailMessageData ToMailMessageData(MailState mail)
    {
        return new MailMessageData
        {
            MailId = mail.MailId,
            SenderAccountId = mail.SenderAccountId,
            SenderCharacterId = mail.SenderCharacterId,
            SenderName = mail.SenderName,
            ReceiverAccountId = mail.ReceiverAccountId,
            ReceiverCharacterId = mail.ReceiverCharacterId,
            ReceiverName = mail.ReceiverName,
            Title = mail.Title,
            Body = mail.Body,
            Zeny = mail.Zeny,
            Attachment = Google.Protobuf.ByteString.CopyFrom(mail.Attachment),
            Opened = mail.Opened
        };
    }

    private static AuctionData ToAuctionData(AuctionState auction)
    {
        return new AuctionData
        {
            AuctionId = auction.AuctionId,
            SellerCharacterId = auction.SellerCharacterId,
            SellerName = auction.SellerName,
            BuyerCharacterId = auction.BuyerCharacterId,
            BuyerName = auction.BuyerName,
            ItemId = auction.ItemId,
            ItemName = auction.ItemName,
            ItemType = auction.ItemType,
            Refine = auction.Refine,
            Attribute = auction.Attribute,
            Price = auction.Price,
            BuyNow = auction.BuyNow,
            Hours = auction.Hours,
            EndTimeUnix = auction.EndTimeUnix,
            ItemPayload = Google.Protobuf.ByteString.CopyFrom(auction.ItemPayload)
        };
    }

    private static bool MatchesAuctionRequest(AuctionState auction, AuctionRequestListRequest request)
    {
        return request.Type switch
        {
            4 => string.IsNullOrWhiteSpace(request.SearchText) ||
                 auction.ItemName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase),
            5 => auction.Price <= request.Price,
            6 => auction.SellerCharacterId == request.CharacterId,
            7 => auction.BuyerCharacterId == request.CharacterId,
            _ => true
        };
    }

    private int CountAuctions(long characterId, bool buy)
    {
        if (characterId <= 0)
        {
            return 0;
        }

        return _auctionsById.Values.Count(auction => buy
            ? auction.BuyerCharacterId == characterId
            : auction.SellerCharacterId == characterId);
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

    private static PetData ToPetData(PetState pet)
    {
        return new PetData
        {
            PetId = pet.PetId,
            AccountId = pet.AccountId,
            CharacterId = pet.CharacterId,
            ClassId = pet.ClassId,
            Level = pet.Level,
            EggItemId = pet.EggItemId,
            EquipItemId = pet.EquipItemId,
            Intimacy = pet.Intimacy,
            Hungry = pet.Hungry,
            RenameFlag = pet.RenameFlag,
            Incubate = pet.Incubate,
            Name = pet.Name,
            Payload = Google.Protobuf.ByteString.CopyFrom(pet.Payload)
        };
    }

    private static PetState ToPetState(PetData pet)
    {
        return new PetState
        {
            PetId = pet.PetId,
            AccountId = pet.AccountId,
            CharacterId = pet.CharacterId,
            ClassId = pet.ClassId,
            Level = pet.Level,
            EggItemId = pet.EggItemId,
            EquipItemId = pet.EquipItemId,
            Intimacy = pet.Intimacy,
            Hungry = pet.Hungry,
            RenameFlag = pet.RenameFlag,
            Incubate = pet.Incubate,
            Name = pet.Name,
            Payload = pet.Payload.ToByteArray()
        };
    }

    private static HomunculusData ToHomunculusData(HomunculusState hom)
    {
        return new HomunculusData
        {
            HomunculusId = hom.HomunculusId,
            CharacterId = hom.CharacterId,
            ClassId = hom.ClassId,
            Name = hom.Name,
            Level = hom.Level,
            Exp = hom.Exp,
            Intimacy = hom.Intimacy,
            Hunger = hom.Hunger,
            Hp = hom.Hp,
            MaxHp = hom.MaxHp,
            Sp = hom.Sp,
            MaxSp = hom.MaxSp,
            Payload = Google.Protobuf.ByteString.CopyFrom(hom.Payload)
        };
    }

    private static HomunculusState ToHomunculusState(HomunculusData hom)
    {
        return new HomunculusState
        {
            HomunculusId = hom.HomunculusId,
            CharacterId = hom.CharacterId,
            ClassId = hom.ClassId,
            Name = hom.Name,
            Level = hom.Level,
            Exp = hom.Exp,
            Intimacy = hom.Intimacy,
            Hunger = hom.Hunger,
            Hp = hom.Hp,
            MaxHp = hom.MaxHp,
            Sp = hom.Sp,
            MaxSp = hom.MaxSp,
            Payload = hom.Payload.ToByteArray()
        };
    }

    private static MercenaryData ToMercenaryData(MercenaryState merc)
    {
        return new MercenaryData
        {
            MercenaryId = merc.MercenaryId,
            CharacterId = merc.CharacterId,
            ClassId = merc.ClassId,
            Hp = merc.Hp,
            Sp = merc.Sp,
            KillCount = merc.KillCount,
            LifeTime = merc.LifeTime,
            Payload = Google.Protobuf.ByteString.CopyFrom(merc.Payload)
        };
    }

    private static MercenaryState ToMercenaryState(MercenaryData merc)
    {
        return new MercenaryState
        {
            MercenaryId = merc.MercenaryId,
            CharacterId = merc.CharacterId,
            ClassId = merc.ClassId,
            Hp = merc.Hp,
            Sp = merc.Sp,
            KillCount = merc.KillCount,
            LifeTime = merc.LifeTime,
            Payload = merc.Payload.ToByteArray()
        };
    }

    private static ElementalData ToElementalData(ElementalState ele)
    {
        return new ElementalData
        {
            ElementalId = ele.ElementalId,
            CharacterId = ele.CharacterId,
            ClassId = ele.ClassId,
            Mode = ele.Mode,
            Hp = ele.Hp,
            Sp = ele.Sp,
            MaxHp = ele.MaxHp,
            MaxSp = ele.MaxSp,
            Attack = ele.Attack,
            Attack2 = ele.Attack2,
            Matk = ele.Matk,
            Aspd = ele.Aspd,
            Def = ele.Def,
            Mdef = ele.Mdef,
            Flee = ele.Flee,
            Hit = ele.Hit,
            LifeTime = ele.LifeTime,
            Payload = Google.Protobuf.ByteString.CopyFrom(ele.Payload)
        };
    }

    private static ElementalState ToElementalState(ElementalData ele)
    {
        return new ElementalState
        {
            ElementalId = ele.ElementalId,
            CharacterId = ele.CharacterId,
            ClassId = ele.ClassId,
            Mode = ele.Mode,
            Hp = ele.Hp,
            Sp = ele.Sp,
            MaxHp = ele.MaxHp,
            MaxSp = ele.MaxSp,
            Attack = ele.Attack,
            Attack2 = ele.Attack2,
            Matk = ele.Matk,
            Aspd = ele.Aspd,
            Def = ele.Def,
            Mdef = ele.Mdef,
            Flee = ele.Flee,
            Hit = ele.Hit,
            LifeTime = ele.LifeTime,
            Payload = ele.Payload.ToByteArray()
        };
    }

    private static ClanData ToClanData(ClanState clan)
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
        data.Alliances.AddRange(clan.Alliances.Select(a => new ClanAllianceData
        {
            Opposition = a.Opposition,
            ClanId = a.ClanId,
            Name = a.Name
        }));
        return data;
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

    private sealed class MailState
    {
        public long MailId { get; init; }
        public int SenderAccountId { get; init; }
        public long SenderCharacterId { get; init; }
        public string SenderName { get; init; } = string.Empty;
        public int ReceiverAccountId { get; init; }
        public long ReceiverCharacterId { get; init; }
        public string ReceiverName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public long Zeny { get; set; }
        public byte[] Attachment { get; set; } = Array.Empty<byte>();
        public bool Opened { get; set; }
    }

    private sealed class AuctionState
    {
        public long AuctionId { get; init; }
        public long SellerCharacterId { get; init; }
        public string SellerName { get; init; } = string.Empty;
        public long BuyerCharacterId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public int ItemId { get; init; }
        public string ItemName { get; init; } = string.Empty;
        public int ItemType { get; init; }
        public int Refine { get; init; }
        public int Attribute { get; init; }
        public int Price { get; set; }
        public int BuyNow { get; init; }
        public int Hours { get; init; }
        public long EndTimeUnix { get; init; }
        public byte[] ItemPayload { get; init; } = Array.Empty<byte>();
    }

    private sealed class PetState
    {
        public int PetId { get; set; }
        public int AccountId { get; set; }
        public int CharacterId { get; set; }
        public int ClassId { get; init; }
        public int Level { get; init; }
        public int EggItemId { get; init; }
        public int EquipItemId { get; init; }
        public int Intimacy { get; set; }
        public int Hungry { get; set; }
        public int RenameFlag { get; init; }
        public bool Incubate { get; init; }
        public string Name { get; init; } = string.Empty;
        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }

    private sealed class HomunculusState
    {
        public int HomunculusId { get; set; }
        public int CharacterId { get; init; }
        public int ClassId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Level { get; init; }
        public long Exp { get; init; }
        public int Intimacy { get; set; }
        public int Hunger { get; set; }
        public int Hp { get; init; }
        public int MaxHp { get; init; }
        public int Sp { get; init; }
        public int MaxSp { get; init; }
        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }

    private sealed class MercenaryState
    {
        public int MercenaryId { get; set; }
        public int CharacterId { get; init; }
        public int ClassId { get; init; }
        public int Hp { get; init; }
        public int Sp { get; init; }
        public int KillCount { get; init; }
        public long LifeTime { get; init; }
        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }

    private sealed class ElementalState
    {
        public int ElementalId { get; set; }
        public int CharacterId { get; init; }
        public int ClassId { get; init; }
        public int Mode { get; init; }
        public int Hp { get; init; }
        public int Sp { get; init; }
        public int MaxHp { get; init; }
        public int MaxSp { get; init; }
        public int Attack { get; init; }
        public int Attack2 { get; init; }
        public int Matk { get; init; }
        public int Aspd { get; init; }
        public int Def { get; init; }
        public int Mdef { get; init; }
        public int Flee { get; init; }
        public int Hit { get; init; }
        public long LifeTime { get; init; }
        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }

    private sealed class ClanState
    {
        public int ClanId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Master { get; init; } = string.Empty;
        public string MapName { get; init; } = string.Empty;
        public int MaxMember { get; init; }
        public int ConnectMember { get; set; }
        public object SyncRoot { get; } = new();
        public List<ClanAllianceState> Alliances { get; } = new();
    }

    private sealed class ClanAllianceState
    {
        public bool Opposition { get; init; }
        public int ClanId { get; init; }
        public string Name { get; init; } = string.Empty;
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

    private static CharacterDataResponse ToCharacterDataResponse(CharEntity character)
    {
        return new CharacterDataResponse
        {
            Character = ToCharacterInfo(character),
            MapId = 0,
            PositionX = character.LastX,
            PositionY = character.LastY,
            PositionZ = 0
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
