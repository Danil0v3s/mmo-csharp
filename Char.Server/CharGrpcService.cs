using Char.Server.Services;
using Core.Server.IPC;
using Core.Server;
using Grpc.Core;
using System.Collections.Concurrent;

namespace Char.Server;

public class CharGrpcService : CharacterService.CharacterServiceBase
{
    private readonly CharServerImpl _charServer;
    private readonly IMapAuthTicketService _mapAuthTicketService;
    private readonly ILoginServerIpcService _loginServerIpc;
    private readonly ILogger<CharGrpcService> _logger;
    private readonly ConcurrentDictionary<int, string[]> _mapServerMaps = new();
    private readonly ConcurrentDictionary<int, int> _mapServerUserCounts = new();
    private readonly ConcurrentDictionary<int, (uint Ip, uint Port)> _mapServerAddresses = new();
    private readonly ConcurrentDictionary<long, byte[]> _statusChangeDataByCharacter = new();
    private readonly ConcurrentDictionary<long, byte[]> _skillCooldownByCharacter = new();
    private readonly ConcurrentDictionary<long, byte[]> _bonusScriptByCharacter = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<long, int>> _fameByType = new();
    private readonly ConcurrentDictionary<int, PartyState> _parties = new();
    private int _nextPartyId = 1000;
    private uint _partyShareLevel = 0;
    private readonly ConcurrentDictionary<int, GuildState> _guilds = new();
    private readonly ConcurrentDictionary<int, Dictionary<int, int>> _guildCastleData = new();
    private int _nextGuildId = 2000;
    private readonly ConcurrentDictionary<int, byte[]> _guildStorageByGuild = new();
    private readonly ConcurrentDictionary<int, byte[]> _accountStorageByAccount = new();
    private readonly ConcurrentDictionary<long, MailState> _mailById = new();
    private readonly ConcurrentDictionary<long, List<long>> _mailByReceiverCharacter = new();
    private int _nextMailId = 5000;
    private readonly ConcurrentDictionary<long, AuctionState> _auctionsById = new();
    private int _nextAuctionId = 7000;
    private readonly ConcurrentDictionary<long, List<QuestEntryState>> _questsByCharacter = new();
    private readonly ConcurrentDictionary<long, List<AchievementEntryState>> _achievementsByCharacter = new();
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
        ILogger<CharGrpcService> logger)
    {
        _charServer = charServer;
        _mapAuthTicketService = mapAuthTicketService;
        _loginServerIpc = loginServerIpc;
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
        return Task.FromResult(BuildCharacterDataResponse(request.CharacterId));
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

    public override Task<CharacterMapAuthResponse> RequestCharacterMapAuth(
        CharacterMapAuthRequest request,
        ServerCallContext context)
    {
        if (_charServer.State != ServerState.Running || request.AccountId <= 0 || request.CharacterId <= 0)
        {
            return Task.FromResult(new CharacterMapAuthResponse
            {
                Success = false,
                ErrorMessage = "Invalid map auth request"
            });
        }

        // Autotrade bypass parity path.
        if (request.Autotrade)
        {
            return Task.FromResult(new CharacterMapAuthResponse
            {
                Success = true,
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                LoginId1 = 0,
                LoginId2 = 0,
                ExpirationTime = 0,
                GroupId = 0,
                ChangingMapServers = false,
                CharacterData = BuildCharacterDataResponse(request.CharacterId)
            });
        }

        if (!_mapAuthTicketService.TryConsumeTicket(
                request.AccountId,
                request.CharacterId,
                request.LoginId1,
                request.LoginId2,
                out var sex,
                out var clientType))
        {
            return Task.FromResult(new CharacterMapAuthResponse
            {
                Success = false,
                ErrorMessage = "Auth ticket missing/expired/mismatch",
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                LoginId1 = request.LoginId1,
                LoginId2 = request.LoginId2
            });
        }

        var characterData = BuildCharacterDataResponse(request.CharacterId);

        return Task.FromResult(new CharacterMapAuthResponse
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
        });
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

        // DB persistence will be wired as repository migration progresses.
        return Task.FromResult(new SaveCharacterStateResponse
        {
            Success = true,
            SaveAck = request.SetOfflineAfterSave
        });
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

    public override Task<StatusChangeDataResponse> RequestStatusChangeData(
        StatusChangeDataRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new StatusChangeDataResponse { Success = false });
        }

        _statusChangeDataByCharacter.TryGetValue(request.CharacterId, out var data);
        return Task.FromResult(new StatusChangeDataResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        });
    }

    public override Task<StatusChangeDataSaveResponse> SaveStatusChangeData(
        StatusChangeDataSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new StatusChangeDataSaveResponse { Success = false });
        }

        _statusChangeDataByCharacter[request.CharacterId] = request.Data.ToByteArray();
        return Task.FromResult(new StatusChangeDataSaveResponse { Success = true });
    }

    public override Task<SkillCooldownLoadResponse> LoadSkillCooldown(
        SkillCooldownLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new SkillCooldownLoadResponse { Success = false });
        }

        _skillCooldownByCharacter.TryGetValue(request.CharacterId, out var data);
        return Task.FromResult(new SkillCooldownLoadResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        });
    }

    public override Task<SkillCooldownSaveResponse> SaveSkillCooldown(
        SkillCooldownSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new SkillCooldownSaveResponse { Success = false });
        }

        _skillCooldownByCharacter[request.CharacterId] = request.Data.ToByteArray();
        return Task.FromResult(new SkillCooldownSaveResponse { Success = true });
    }

    public override async Task<CharacterOnlineStateResponse> SetCharacterOffline(
        CharacterOnlineStateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return new CharacterOnlineStateResponse { Success = false };
        }

        await _charServer.ForceDisconnectAccountAsync(request.AccountId);
        return new CharacterOnlineStateResponse { Success = true };
    }

    public override Task<CharacterOnlineStateResponse> SetCharacterOnline(
        CharacterOnlineStateRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new CharacterOnlineStateResponse
        {
            Success = request.AccountId > 0
        });
    }

    public override Task<SetAllCharactersOfflineResponse> SetAllCharactersOffline(
        SetAllCharactersOfflineRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Received map-server all-offline request from map server {MapServerId}", request.MapServerId);
        return Task.FromResult(new SetAllCharactersOfflineResponse { Success = true });
    }

    public override Task<RemoveFriendResponse> RequestRemoveFriend(
        RemoveFriendRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new RemoveFriendResponse
        {
            Success = request.CharacterId > 0 && request.FriendCharacterId > 0
        });
    }

    public override Task<CharacterNameResponse> RequestCharacterName(
        CharacterNameRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new CharacterNameResponse { Success = false, Name = string.Empty });
        }

        var characterData = BuildCharacterDataResponse(request.CharacterId);
        return Task.FromResult(new CharacterNameResponse
        {
            Success = true,
            Name = characterData.Character?.Name ?? string.Empty
        });
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

    public override Task<DivorceResponse> RequestDivorce(
        DivorceRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new DivorceResponse
        {
            Success = request.CharacterId > 0
        });
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
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new FameUpdateResponse { Success = false });
        }

        var fame = _fameByType.GetOrAdd(request.FameType, _ => new ConcurrentDictionary<long, int>());
        fame[request.CharacterId] = request.Value;

        return Task.FromResult(new FameUpdateResponse { Success = true });
    }

    public override Task<FameListResponse> RequestFameList(
        FameListRequest request,
        ServerCallContext context)
    {
        var response = new FameListResponse { Success = true };
        if (_fameByType.TryGetValue(request.FameType, out var fame))
        {
            foreach (var entry in fame.OrderByDescending(e => e.Value).Take(10))
            {
                response.Entries.Add(new FameEntry
                {
                    CharacterId = entry.Key,
                    Value = entry.Value
                });
            }
        }

        return Task.FromResult(response);
    }

    public override Task<BonusScriptGetResponse> GetBonusScript(
        BonusScriptGetRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new BonusScriptGetResponse { Success = false });
        }

        _bonusScriptByCharacter.TryGetValue(request.CharacterId, out var data);
        return Task.FromResult(new BonusScriptGetResponse
        {
            Success = true,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        });
    }

    public override Task<BonusScriptSaveResponse> SaveBonusScript(
        BonusScriptSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new BonusScriptSaveResponse { Success = false });
        }

        _bonusScriptByCharacter[request.CharacterId] = request.Data.ToByteArray();
        return Task.FromResult(new BonusScriptSaveResponse { Success = true });
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

    public override Task<PartyCreateResponse> PartyCreate(
        PartyCreateRequest request,
        ServerCallContext context)
    {
        if (request.LeaderAccountId <= 0 || request.LeaderCharacterId <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return Task.FromResult(new PartyCreateResponse
            {
                Success = false,
                ErrorMessage = "Invalid party create request"
            });
        }

        var partyId = Interlocked.Increment(ref _nextPartyId);
        var party = new PartyState
        {
            PartyId = partyId,
            Name = request.Name.Trim(),
            Item = request.Item,
            Item2 = request.Item2,
            LeaderCharacterId = request.LeaderCharacterId
        };

        party.Members[request.LeaderCharacterId] = new PartyMemberState
        {
            AccountId = request.LeaderAccountId,
            CharacterId = request.LeaderCharacterId,
            Name = request.LeaderName ?? string.Empty,
            ClassId = request.LeaderClassId,
            MapName = request.LeaderMapName ?? string.Empty,
            Level = request.LeaderLevel,
            Online = true
        };

        _parties[partyId] = party;

        return Task.FromResult(new PartyCreateResponse
        {
            Success = true,
            PartyId = partyId
        });
    }

    public override Task<PartyInfoResponse> PartyInfo(
        PartyInfoRequest request,
        ServerCallContext context)
    {
        if (!_parties.TryGetValue(request.PartyId, out var party))
        {
            return Task.FromResult(new PartyInfoResponse { Success = false });
        }

        PartyInfoData data;
        lock (party.SyncRoot)
        {
            data = new PartyInfoData
            {
                PartyId = party.PartyId,
                Name = party.Name,
                Item = party.Item,
                Item2 = party.Item2,
                LeaderCharacterId = party.LeaderCharacterId
            };

            data.Members.AddRange(party.Members.Values.Select(ToPartyMemberInfo));
        }

        return Task.FromResult(new PartyInfoResponse
        {
            Success = true,
            Party = data
        });
    }

    public override Task<PartyAddMemberResponse> PartyAddMember(
        PartyAddMemberRequest request,
        ServerCallContext context)
    {
        if (!_parties.TryGetValue(request.PartyId, out var party) ||
            request.AccountId <= 0 ||
            request.CharacterId <= 0)
        {
            return Task.FromResult(new PartyAddMemberResponse { Success = false });
        }

        lock (party.SyncRoot)
        {
            party.Members[request.CharacterId] = new PartyMemberState
            {
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                Name = request.Name ?? string.Empty,
                ClassId = request.ClassId,
                MapName = request.MapName ?? string.Empty,
                Level = request.Level,
                Online = true
            };
        }

        return Task.FromResult(new PartyAddMemberResponse { Success = true });
    }

    public override Task<PartyChangeOptionResponse> PartyChangeOption(
        PartyChangeOptionRequest request,
        ServerCallContext context)
    {
        if (!_parties.TryGetValue(request.PartyId, out var party))
        {
            return Task.FromResult(new PartyChangeOptionResponse { Success = false });
        }

        lock (party.SyncRoot)
        {
            party.Item = request.Item;
            party.Exp = request.Exp;
        }

        return Task.FromResult(new PartyChangeOptionResponse { Success = true });
    }

    public override Task<PartyLeaveResponse> PartyLeave(
        PartyLeaveRequest request,
        ServerCallContext context)
    {
        if (!_parties.TryGetValue(request.PartyId, out var party))
        {
            return Task.FromResult(new PartyLeaveResponse { Success = false });
        }

        lock (party.SyncRoot)
        {
            party.Members.Remove(request.CharacterId);

            if (party.LeaderCharacterId == request.CharacterId)
            {
                party.LeaderCharacterId = party.Members.Keys.FirstOrDefault();
            }

            if (party.Members.Count == 0)
            {
                _parties.TryRemove(request.PartyId, out _);
            }
        }

        return Task.FromResult(new PartyLeaveResponse { Success = true });
    }

    public override Task<PartyChangeMapResponse> PartyChangeMap(
        PartyChangeMapRequest request,
        ServerCallContext context)
    {
        if (!_parties.TryGetValue(request.PartyId, out var party))
        {
            return Task.FromResult(new PartyChangeMapResponse { Success = false });
        }

        lock (party.SyncRoot)
        {
            if (!party.Members.TryGetValue(request.CharacterId, out var member))
            {
                return Task.FromResult(new PartyChangeMapResponse { Success = false });
            }

            member.MapName = request.MapName ?? string.Empty;
            member.Level = request.Level;
            member.Online = request.Online;
        }

        return Task.FromResult(new PartyChangeMapResponse { Success = true });
    }

    public override Task<PartyBreakResponse> PartyBreak(
        PartyBreakRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new PartyBreakResponse
        {
            Success = _parties.TryRemove(request.PartyId, out _)
        });
    }

    public override Task<PartyMessageResponse> PartyMessage(
        PartyMessageRequest request,
        ServerCallContext context)
    {
        var success = _parties.ContainsKey(request.PartyId) && !string.IsNullOrWhiteSpace(request.Message);
        return Task.FromResult(new PartyMessageResponse { Success = success });
    }

    public override Task<PartyLeaderChangeResponse> PartyLeaderChange(
        PartyLeaderChangeRequest request,
        ServerCallContext context)
    {
        if (!_parties.TryGetValue(request.PartyId, out var party))
        {
            return Task.FromResult(new PartyLeaderChangeResponse { Success = false });
        }

        lock (party.SyncRoot)
        {
            if (!party.Members.ContainsKey(request.CharacterId))
            {
                return Task.FromResult(new PartyLeaderChangeResponse { Success = false });
            }

            party.LeaderCharacterId = request.CharacterId;
        }

        return Task.FromResult(new PartyLeaderChangeResponse { Success = true });
    }

    public override Task<PartyShareLevelResponse> PartyShareLevel(
        PartyShareLevelRequest request,
        ServerCallContext context)
    {
        _partyShareLevel = request.ShareLevel;
        return Task.FromResult(new PartyShareLevelResponse { Success = true });
    }

    public override Task<GuildCreateResponse> GuildCreate(
        GuildCreateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0 || request.MasterCharacterId <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return Task.FromResult(new GuildCreateResponse
            {
                Success = false,
                ErrorMessage = "Invalid guild create request"
            });
        }

        var guildId = Interlocked.Increment(ref _nextGuildId);
        var guild = new GuildState
        {
            GuildId = guildId,
            Name = request.Name.Trim(),
            MasterCharacterId = request.MasterCharacterId,
            Level = 1,
            MaxMember = 16
        };

        guild.Members[request.MasterCharacterId] = new GuildMemberState
        {
            AccountId = request.AccountId,
            CharacterId = request.MasterCharacterId,
            Name = request.MasterName ?? string.Empty,
            ClassId = request.MasterClassId,
            Level = request.MasterLevel,
            Online = true,
            PositionName = "Master"
        };

        _guilds[guildId] = guild;

        return Task.FromResult(new GuildCreateResponse
        {
            Success = true,
            GuildId = guildId
        });
    }

    public override Task<GuildInfoResponse> GuildInfo(
        GuildInfoRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildInfoResponse { Success = false });
        }

        GuildInfoData data;
        lock (guild.SyncRoot)
        {
            data = ToGuildInfoData(guild);
        }

        return Task.FromResult(new GuildInfoResponse
        {
            Success = true,
            Guild = data
        });
    }

    public override Task<GuildAddMemberResponse> GuildAddMember(
        GuildAddMemberRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild) ||
            request.AccountId <= 0 ||
            request.CharacterId <= 0)
        {
            return Task.FromResult(new GuildAddMemberResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.Members[request.CharacterId] = new GuildMemberState
            {
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                Name = request.Name ?? string.Empty,
                ClassId = request.ClassId,
                Level = request.Level,
                Online = true,
                PositionName = "Member"
            };
        }

        return Task.FromResult(new GuildAddMemberResponse { Success = true });
    }

    public override Task<GuildMasterChangeResponse> GuildMasterChange(
        GuildMasterChangeRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildMasterChangeResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            var newMaster = guild.Members.Values
                .FirstOrDefault(member => string.Equals(member.Name, request.MasterName, StringComparison.OrdinalIgnoreCase));
            if (newMaster == null)
            {
                return Task.FromResult(new GuildMasterChangeResponse { Success = false });
            }

            guild.MasterCharacterId = newMaster.CharacterId;
            newMaster.PositionName = "Master";
        }

        return Task.FromResult(new GuildMasterChangeResponse { Success = true });
    }

    public override Task<GuildLeaveResponse> GuildLeave(
        GuildLeaveRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildLeaveResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.Members.Remove(request.CharacterId);

            if (guild.MasterCharacterId == request.CharacterId)
            {
                guild.MasterCharacterId = guild.Members.Keys.FirstOrDefault();
            }

            if (guild.Members.Count == 0)
            {
                _guilds.TryRemove(request.GuildId, out _);
            }
        }

        return Task.FromResult(new GuildLeaveResponse { Success = true });
    }

    public override Task<GuildChangeMemberInfoShortResponse> GuildChangeMemberInfoShort(
        GuildChangeMemberInfoShortRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildChangeMemberInfoShortResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            if (!guild.Members.TryGetValue(request.CharacterId, out var member))
            {
                return Task.FromResult(new GuildChangeMemberInfoShortResponse { Success = false });
            }

            member.Online = request.Online;
            member.Level = request.Level;
            member.ClassId = request.ClassId;
        }

        return Task.FromResult(new GuildChangeMemberInfoShortResponse { Success = true });
    }

    public override Task<GuildBreakResponse> GuildBreak(
        GuildBreakRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new GuildBreakResponse
        {
            Success = _guilds.TryRemove(request.GuildId, out _)
        });
    }

    public override Task<GuildMessageResponse> GuildMessage(
        GuildMessageRequest request,
        ServerCallContext context)
    {
        var success = _guilds.ContainsKey(request.GuildId) && !string.IsNullOrWhiteSpace(request.Message);
        return Task.FromResult(new GuildMessageResponse { Success = success });
    }

    public override Task<GuildBasicInfoChangeResponse> GuildBasicInfoChange(
        GuildBasicInfoChangeRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildBasicInfoChangeResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            switch (request.Type)
            {
                case 1:
                    guild.Name = request.Data.ToStringUtf8();
                    break;
                case 2:
                    if (request.Data.Length >= 4)
                    {
                        guild.Level = BitConverter.ToInt32(request.Data.ToByteArray(), 0);
                    }
                    break;
            }
        }

        return Task.FromResult(new GuildBasicInfoChangeResponse { Success = true });
    }

    public override Task<GuildMemberInfoChangeResponse> GuildMemberInfoChange(
        GuildMemberInfoChangeRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildMemberInfoChangeResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            if (!guild.Members.TryGetValue(request.CharacterId, out var member))
            {
                return Task.FromResult(new GuildMemberInfoChangeResponse { Success = false });
            }

            switch (request.Type)
            {
                case 1:
                    member.PositionName = request.Data.ToStringUtf8();
                    break;
                case 2:
                    if (request.Data.Length >= 4)
                    {
                        member.ClassId = BitConverter.ToInt32(request.Data.ToByteArray(), 0);
                    }
                    break;
            }
        }

        return Task.FromResult(new GuildMemberInfoChangeResponse { Success = true });
    }

    public override Task<GuildPositionChangeResponse> GuildPositionChange(
        GuildPositionChangeRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildPositionChangeResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.Positions[request.Index] = new GuildPositionState
            {
                Index = request.Index,
                Name = request.Position?.Name ?? string.Empty,
                Mode = request.Position?.Mode ?? 0,
                ExpMode = request.Position?.ExpMode ?? 0
            };
        }

        return Task.FromResult(new GuildPositionChangeResponse { Success = true });
    }

    public override Task<GuildSkillUpResponse> GuildSkillUp(
        GuildSkillUpRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildSkillUpResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.Skills[request.SkillId] = Math.Min(guild.Skills.GetValueOrDefault(request.SkillId) + 1, request.Max);
        }

        return Task.FromResult(new GuildSkillUpResponse { Success = true });
    }

    public override Task<GuildAllianceResponse> GuildAlliance(
        GuildAllianceRequest request,
        ServerCallContext context)
    {
        var ok = _guilds.ContainsKey(request.GuildId1) && _guilds.ContainsKey(request.GuildId2);
        return Task.FromResult(new GuildAllianceResponse { Success = ok });
    }

    public override Task<GuildNoticeResponse> GuildNotice(
        GuildNoticeRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildNoticeResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.Notice1 = request.Notice1 ?? string.Empty;
            guild.Notice2 = request.Notice2 ?? string.Empty;
        }

        return Task.FromResult(new GuildNoticeResponse { Success = true });
    }

    public override Task<GuildEmblemResponse> GuildEmblem(
        GuildEmblemRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildEmblemResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.EmblemData = request.Data.ToByteArray();
            guild.EmblemVersion++;
        }

        return Task.FromResult(new GuildEmblemResponse { Success = true });
    }

    public override Task<GuildCastleDataLoadResponse> GuildCastleDataLoad(
        GuildCastleDataLoadRequest request,
        ServerCallContext context)
    {
        var response = new GuildCastleDataLoadResponse { Success = true };
        foreach (var castleId in request.CastleIds)
        {
            if (_guildCastleData.TryGetValue(castleId, out var values))
            {
                foreach (var value in values)
                {
                    response.Values[(castleId << 8) + value.Key] = value.Value;
                }
            }
        }

        return Task.FromResult(response);
    }

    public override Task<GuildCastleDataSaveResponse> GuildCastleDataSave(
        GuildCastleDataSaveRequest request,
        ServerCallContext context)
    {
        var castle = _guildCastleData.GetOrAdd(request.CastleId, _ => new Dictionary<int, int>());
        castle[request.Index] = request.Value;
        return Task.FromResult(new GuildCastleDataSaveResponse { Success = true });
    }

    public override Task<GuildEmblemVersionResponse> GuildEmblemVersion(
        GuildEmblemVersionRequest request,
        ServerCallContext context)
    {
        if (!_guilds.TryGetValue(request.GuildId, out var guild))
        {
            return Task.FromResult(new GuildEmblemVersionResponse { Success = false });
        }

        lock (guild.SyncRoot)
        {
            guild.EmblemVersion = request.Version;
        }

        return Task.FromResult(new GuildEmblemVersionResponse { Success = true });
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

    public override Task<QuestLoadResponse> QuestLoad(
        QuestLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new QuestLoadResponse { Success = false });
        }

        var response = new QuestLoadResponse { Success = true };
        if (_questsByCharacter.TryGetValue(request.CharacterId, out var quests))
        {
            lock (quests)
            {
                response.Quests.AddRange(
                    quests.OrderBy(entry => entry.State == 2 ? 1 : 0)
                        .Select(ToQuestEntryData));
            }
        }

        return Task.FromResult(response);
    }

    public override Task<QuestSaveResponse> QuestSave(
        QuestSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new QuestSaveResponse { Success = false });
        }

        var entries = request.Quests.Select(ToQuestEntryState).ToList();
        _questsByCharacter[request.CharacterId] = entries;
        return Task.FromResult(new QuestSaveResponse { Success = true });
    }

    public override Task<AchievementLoadResponse> AchievementLoad(
        AchievementLoadRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new AchievementLoadResponse { Success = false });
        }

        var response = new AchievementLoadResponse { Success = true };
        if (_achievementsByCharacter.TryGetValue(request.CharacterId, out var achievements))
        {
            lock (achievements)
            {
                response.Achievements.AddRange(
                    achievements.OrderBy(entry => entry.CompletedUnix > 0 ? 1 : 0)
                        .Select(ToAchievementEntryData));
            }
        }

        return Task.FromResult(response);
    }

    public override Task<AchievementSaveResponse> AchievementSave(
        AchievementSaveRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0)
        {
            return Task.FromResult(new AchievementSaveResponse { Success = false });
        }

        var entries = request.Achievements.Select(ToAchievementEntryState).ToList();
        _achievementsByCharacter[request.CharacterId] = entries;
        return Task.FromResult(new AchievementSaveResponse { Success = true });
    }

    public override Task<AchievementRewardResponse> AchievementReward(
        AchievementRewardRequest request,
        ServerCallContext context)
    {
        if (request.CharacterId <= 0 || request.AchievementId <= 0)
        {
            return Task.FromResult(new AchievementRewardResponse { Success = false, RewardedUnix = 0 });
        }

        if (!_achievementsByCharacter.TryGetValue(request.CharacterId, out var achievements))
        {
            return Task.FromResult(new AchievementRewardResponse { Success = false, RewardedUnix = 0 });
        }

        lock (achievements)
        {
            var achievement = achievements.FirstOrDefault(entry => entry.AchievementId == request.AchievementId);
            if (achievement == null || achievement.CompletedUnix <= 0 || achievement.RewardedUnix > 0)
            {
                return Task.FromResult(new AchievementRewardResponse { Success = false, RewardedUnix = 0 });
            }

            achievement.RewardedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Task.FromResult(new AchievementRewardResponse
            {
                Success = true,
                RewardedUnix = achievement.RewardedUnix
            });
        }
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

    private static QuestEntryData ToQuestEntryData(QuestEntryState quest)
    {
        var data = new QuestEntryData
        {
            QuestId = quest.QuestId,
            TimeUnix = quest.TimeUnix,
            State = quest.State
        };
        data.Counts.AddRange(quest.Counts);
        return data;
    }

    private static QuestEntryState ToQuestEntryState(QuestEntryData quest)
    {
        return new QuestEntryState
        {
            QuestId = quest.QuestId,
            TimeUnix = quest.TimeUnix,
            State = quest.State,
            Counts = quest.Counts.ToList()
        };
    }

    private static AchievementEntryData ToAchievementEntryData(AchievementEntryState achievement)
    {
        var data = new AchievementEntryData
        {
            AchievementId = achievement.AchievementId,
            CompletedUnix = achievement.CompletedUnix,
            RewardedUnix = achievement.RewardedUnix,
            Score = achievement.Score
        };
        data.Counts.AddRange(achievement.Counts);
        return data;
    }

    private static AchievementEntryState ToAchievementEntryState(AchievementEntryData achievement)
    {
        return new AchievementEntryState
        {
            AchievementId = achievement.AchievementId,
            CompletedUnix = achievement.CompletedUnix,
            RewardedUnix = achievement.RewardedUnix,
            Score = achievement.Score,
            Counts = achievement.Counts.ToList()
        };
    }

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

    private static PartyMemberInfo ToPartyMemberInfo(PartyMemberState member)
    {
        return new PartyMemberInfo
        {
            AccountId = member.AccountId,
            CharacterId = member.CharacterId,
            Name = member.Name,
            ClassId = member.ClassId,
            MapName = member.MapName,
            Level = member.Level,
            Online = member.Online
        };
    }

    private sealed class PartyState
    {
        public int PartyId { get; init; }
        public string Name { get; set; } = string.Empty;
        public int Item { get; set; }
        public int Item2 { get; set; }
        public int Exp { get; set; }
        public long LeaderCharacterId { get; set; }
        public object SyncRoot { get; } = new();
        public Dictionary<long, PartyMemberState> Members { get; } = new();
    }

    private sealed class PartyMemberState
    {
        public int AccountId { get; init; }
        public long CharacterId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int ClassId { get; init; }
        public string MapName { get; set; } = string.Empty;
        public uint Level { get; set; }
        public bool Online { get; set; }
    }

    private static GuildInfoData ToGuildInfoData(GuildState guild)
    {
        var data = new GuildInfoData
        {
            GuildId = guild.GuildId,
            Name = guild.Name,
            Level = guild.Level,
            MaxMember = guild.MaxMember,
            MasterCharacterId = (int)Math.Clamp(guild.MasterCharacterId, int.MinValue, int.MaxValue),
            EmblemVersion = guild.EmblemVersion,
            EmblemData = Google.Protobuf.ByteString.CopyFrom(guild.EmblemData ?? Array.Empty<byte>()),
            Notice1 = guild.Notice1,
            Notice2 = guild.Notice2
        };

        data.Members.AddRange(guild.Members.Values.Select(member => new GuildMemberInfo
        {
            AccountId = member.AccountId,
            CharacterId = member.CharacterId,
            Name = member.Name,
            ClassId = member.ClassId,
            Level = member.Level,
            Online = member.Online,
            PositionName = member.PositionName
        }));

        data.Positions.AddRange(guild.Positions.Values.OrderBy(position => position.Index).Select(position => new GuildPositionInfo
        {
            Index = position.Index,
            Name = position.Name,
            Mode = position.Mode,
            ExpMode = position.ExpMode
        }));

        return data;
    }

    private sealed class GuildState
    {
        public int GuildId { get; init; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int MaxMember { get; set; }
        public long MasterCharacterId { get; set; }
        public int EmblemVersion { get; set; }
        public byte[] EmblemData { get; set; } = Array.Empty<byte>();
        public string Notice1 { get; set; } = string.Empty;
        public string Notice2 { get; set; } = string.Empty;
        public object SyncRoot { get; } = new();
        public Dictionary<long, GuildMemberState> Members { get; } = new();
        public Dictionary<int, GuildPositionState> Positions { get; } = new();
        public Dictionary<uint, int> Skills { get; } = new();
    }

    private sealed class GuildMemberState
    {
        public int AccountId { get; init; }
        public long CharacterId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int ClassId { get; set; }
        public uint Level { get; set; }
        public bool Online { get; set; }
        public string PositionName { get; set; } = string.Empty;
    }

    private sealed class GuildPositionState
    {
        public int Index { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Mode { get; init; }
        public int ExpMode { get; init; }
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

    private sealed class QuestEntryState
    {
        public int QuestId { get; init; }
        public long TimeUnix { get; init; }
        public int State { get; init; }
        public List<int> Counts { get; init; } = new();
    }

    private sealed class AchievementEntryState
    {
        public int AchievementId { get; init; }
        public List<int> Counts { get; init; } = new();
        public long CompletedUnix { get; init; }
        public long RewardedUnix { get; set; }
        public int Score { get; init; }
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

    private static CharacterDataResponse BuildCharacterDataResponse(long characterId)
    {
        return new CharacterDataResponse
        {
            Character = new CharacterInfo
            {
                CharacterId = characterId,
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
