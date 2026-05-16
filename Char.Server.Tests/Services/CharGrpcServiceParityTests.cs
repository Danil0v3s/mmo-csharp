using Char.Server.Services;
using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Services;

public class CharGrpcServiceParityTests
{
    [Fact]
    public async Task GetCharacterList_ShouldFilterDeletedAndOrderBySlot()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1, AccountId = 777, Name = "Slot2", CharNum = 2, DeleteDate = 0 },
            new CharEntity { CharId = 2, AccountId = 777, Name = "Deleted", CharNum = 0, DeleteDate = 10 },
            new CharEntity { CharId = 3, AccountId = 777, Name = "Slot1", CharNum = 1, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.GetCharacterList(new CharacterListRequest { AccountId = 777 }, TestServerCallContext.Instance);

        Assert.Equal(2, response.Characters.Count);
        Assert.Equal("Slot1", response.Characters[0].Name);
        Assert.Equal("Slot2", response.Characters[1].Name);
    }

    [Fact]
    public async Task GetCharacterData_AndName_ShouldReturnNotFoundForMissingCharacter()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));

        var data = await service.GetCharacterData(new CharacterDataRequest { CharacterId = 9999 }, TestServerCallContext.Instance);
        var name = await service.RequestCharacterName(new CharacterNameRequest { CharacterId = 9999 }, TestServerCallContext.Instance);

        Assert.Null(data.Character);
        Assert.False(name.Success);
        Assert.Equal(string.Empty, name.Name);
    }

    [Fact]
    public async Task CreateCharacter_ShouldRejectDuplicateName()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 41, AccountId = 100, Name = "TakenName", CharNum = 0, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.CreateCharacter(new CreateCharacterRequest
        {
            AccountId = 100,
            Name = "TakenName",
            ClassId = 0
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task CreateCharacter_ShouldRejectWhenSlotsAreFull()
    {
        var seed = Enumerable.Range(0, 9)
            .Select(i => new CharEntity
            {
                CharId = 1000 + i,
                AccountId = 200,
                Name = $"Char{i}",
                CharNum = (byte)i,
                DeleteDate = 0
            })
            .ToArray();

        var repo = new InMemoryCharacterRepository(seed);
        var (service, _) = CreateService(repo, config => { config.Char.CharPerAccount = 9; });

        var response = await service.CreateCharacter(new CreateCharacterRequest
        {
            AccountId = 200,
            Name = "NewChar",
            ClassId = 0
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task CreateCharacter_ShouldCreateInFirstAvailableSlot()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 51, AccountId = 300, Name = "Slot0", CharNum = 0, DeleteDate = 0 },
            new CharEntity { CharId = 52, AccountId = 300, Name = "Slot2", CharNum = 2, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo, config =>
        {
            config.Char.CharPerAccount = 9;
            config.StartPoint = [new StartPoint { Map = "prontera", X = 100, Y = 200 }];
        });

        var response = await service.CreateCharacter(new CreateCharacterRequest
        {
            AccountId = 300,
            Name = "FreshChar",
            ClassId = 1
        }, TestServerCallContext.Instance);

        Assert.True(response.Success);
        Assert.Equal("FreshChar", response.Character.Name);

        var characters = await repo.GetByAccountIdAsync(300);
        var created = characters.Single(c => c.Name == "FreshChar");
        Assert.Equal(1, created.CharNum);
    }

    [Fact]
    public async Task RequestCharacterMapAuth_Autotrade_ShouldSucceedWithoutTicket()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 61, AccountId = 400, Name = "Trader", CharNum = 0, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.RequestCharacterMapAuth(new CharacterMapAuthRequest
        {
            AccountId = 400,
            CharacterId = 61,
            Autotrade = true
        }, TestServerCallContext.Instance);

        Assert.True(response.Success);
        Assert.Equal(61, response.CharacterData.Character.CharacterId);
    }

    [Fact]
    public async Task RequestMapServerChange_ShouldRejectInvalidRequest()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.RequestMapServerChange(new MapServerChangeRequest
        {
            AccountId = 0,
            CharacterId = 0,
            LoginId1 = 0,
            LoginId2 = 0,
            MapName = ""
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task RequestMapServerChange_ShouldRejectWhenNoMapConnection()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.RequestMapServerChange(new MapServerChangeRequest
        {
            AccountId = 1,
            CharacterId = 2,
            LoginId1 = 3,
            LoginId2 = 4,
            MapName = "prontera",
            TtlSeconds = 60
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
        Assert.Contains("No active map server connection", response.ErrorMessage);
    }

    [Fact]
    public async Task MapServerRegistryUserCount_ShouldReturnFalseBeforeRegistration_AndTrueAfter()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));

        var before = await service.GetMapServerUserCount(new MapServerUserCountRequest { MapServerId = 88 }, TestServerCallContext.Instance);
        var set = await service.RegisterMapServerUserCount(new MapServerUserCountUpdateRequest { MapServerId = 88, UserCount = 123 }, TestServerCallContext.Instance);
        var after = await service.GetMapServerUserCount(new MapServerUserCountRequest { MapServerId = 88 }, TestServerCallContext.Instance);

        Assert.False(before.Success);
        Assert.True(set.Success);
        Assert.True(after.Success);
        Assert.Equal(123, after.UserCount);
    }

    [Fact]
    public async Task RequestCharacterMapAuth_ShouldConsumeTicketAndRejectReplay()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 101, AccountId = 2000000, Name = "ReplayGuard", CharNum = 0, DeleteDate = 0 }
        ]);

        var (service, ticketService) = CreateService(repo);
        ticketService.IssueTicket(
            accountId: 2000000,
            characterId: 101,
            loginId1: 11,
            loginId2: 22,
            sex: 0,
            clientType: 0,
            ttlSeconds: 60);

        var request = new CharacterMapAuthRequest
        {
            AccountId = 2000000,
            CharacterId = 101,
            LoginId1 = 11,
            LoginId2 = 22
        };

        var first = await service.RequestCharacterMapAuth(request, TestServerCallContext.Instance);
        var replay = await service.RequestCharacterMapAuth(request, TestServerCallContext.Instance);

        Assert.True(first.Success);
        Assert.False(replay.Success);
    }

    [Fact]
    public async Task RequestCharacterMapAuth_ShouldRejectInvalidRequest()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.RequestCharacterMapAuth(new CharacterMapAuthRequest
        {
            AccountId = 0,
            CharacterId = 0
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task RequestCharacterMapAuth_ShouldRejectWhenCharacterNotFoundAfterTicketConsume()
    {
        var (service, ticketService) = CreateService(new InMemoryCharacterRepository([]));
        ticketService.IssueTicket(
            accountId: 5000,
            characterId: 9000,
            loginId1: 55,
            loginId2: 66,
            sex: 0,
            clientType: 0,
            ttlSeconds: 60);

        var response = await service.RequestCharacterMapAuth(new CharacterMapAuthRequest
        {
            AccountId = 5000,
            CharacterId = 9000,
            LoginId1 = 55,
            LoginId2 = 66
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
        Assert.Contains("Character not found", response.ErrorMessage);
    }

    [Fact]
    public async Task SetCharacterOnline_ShouldPersistOnlineFlag()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 201, AccountId = 2000001, Name = "OnlineMe", CharNum = 0, Online = 0, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.SetCharacterOnline(new CharacterOnlineStateRequest
        {
            AccountId = 2000001,
            CharacterId = 201
        }, TestServerCallContext.Instance);

        var stored = await repo.GetByIdAsync(201);
        Assert.True(response.Success);
        Assert.NotNull(stored);
        Assert.Equal(1, stored!.Online);
    }

    [Fact]
    public async Task SetCharacterOnline_ShouldRejectInvalidRequest()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.SetCharacterOnline(new CharacterOnlineStateRequest
        {
            AccountId = 0,
            CharacterId = 0
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SetCharacterOffline_ShouldRejectInvalidRequest()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.SetCharacterOffline(new CharacterOnlineStateRequest
        {
            AccountId = 0,
            CharacterId = 0
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SetAllCharactersOffline_ShouldClearOnlineFlags()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 301, AccountId = 2000002, Name = "A", CharNum = 0, Online = 1, DeleteDate = 0 },
            new CharEntity { CharId = 302, AccountId = 2000003, Name = "B", CharNum = 1, Online = 1, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.SetAllCharactersOffline(new SetAllCharactersOfflineRequest
        {
            MapServerId = 1
        }, TestServerCallContext.Instance);

        var online = await repo.GetOnlineCharactersAsync();
        Assert.True(response.Success);
        Assert.Empty(online);
    }

    [Fact]
    public async Task SetCharacterOffline_WithCharacterIdZero_ShouldSetAllAccountCharactersOffline()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 305, AccountId = 2000009, Name = "C", CharNum = 0, Online = 1, DeleteDate = 0 },
            new CharEntity { CharId = 306, AccountId = 2000009, Name = "D", CharNum = 1, Online = 1, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.SetCharacterOffline(new CharacterOnlineStateRequest
        {
            AccountId = 2000009,
            CharacterId = 0
        }, TestServerCallContext.Instance);

        var byAccount = await repo.GetByAccountIdAsync(2000009);
        Assert.True(response.Success);
        Assert.All(byAccount, c => Assert.Equal(0, c.Online));
    }

    [Fact]
    public async Task SetCharacterOffline_WithSpecificCharacterNotOnline_ShouldStillSucceed()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 307, AccountId = 2000010, Name = "E", CharNum = 0, Online = 0, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.SetCharacterOffline(new CharacterOnlineStateRequest
        {
            AccountId = 2000010,
            CharacterId = 307
        }, TestServerCallContext.Instance);

        Assert.True(response.Success);
    }

    [Fact]
    public async Task SaveCharacterState_WithSetOfflineAfterSave_ShouldPersistOfflineAndAck()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 401, AccountId = 2000004, Name = "Saver", CharNum = 0, Online = 1, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.SaveCharacterState(new SaveCharacterStateRequest
        {
            AccountId = 2000004,
            CharacterId = 401,
            SetOfflineAfterSave = true
        }, TestServerCallContext.Instance);

        var stored = await repo.GetByIdAsync(401);
        Assert.True(response.Success);
        Assert.True(response.SaveAck);
        Assert.NotNull(stored);
        Assert.Equal(0, stored!.Online);
        Assert.NotNull(stored.LastLogin);
    }

    [Fact]
    public async Task SaveCharacterState_ShouldRejectInvalidRequest()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.SaveCharacterState(new SaveCharacterStateRequest
        {
            AccountId = 0,
            CharacterId = 0
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
        Assert.False(response.SaveAck);
    }

    [Fact]
    public async Task SaveCharacterState_ShouldRejectWhenCharacterNotFound()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));
        var response = await service.SaveCharacterState(new SaveCharacterStateRequest
        {
            AccountId = 1234,
            CharacterId = 7777,
            SetOfflineAfterSave = true
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
        Assert.False(response.SaveAck);
    }

    [Fact]
    public async Task DeleteCharacter_ShouldReject_WhenPartyRestrictionEnabledAndCharacterInParty()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 501, AccountId = 2000005, Name = "PartyLocked", CharNum = 0, DeleteDate = 0, PartyId = 999
            }
        ]);

        var (service, _) = CreateService(repo, config =>
        {
            config.Char.CharDeleteRestriction = 0x01;
        });

        var response = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 2000005,
            CharacterId = 501
        }, TestServerCallContext.Instance);

        var stored = await repo.GetByIdAsync(501);
        Assert.False(response.Success);
        Assert.NotNull(stored);
        Assert.Equal<uint>(0, stored!.DeleteDate);
    }

    [Fact]
    public async Task DeleteCharacter_ShouldReject_InvalidRequestOrMismatchedAccount()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 701, AccountId = 7000, Name = "Owned", CharNum = 0, DeleteDate = 0 }
        ]);

        var (service, _) = CreateService(repo);
        var invalid = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 0,
            CharacterId = 0
        }, TestServerCallContext.Instance);
        var mismatched = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 7001,
            CharacterId = 701
        }, TestServerCallContext.Instance);

        Assert.False(invalid.Success);
        Assert.False(mismatched.Success);
    }

    [Fact]
    public async Task DeleteCharacter_ShouldReject_WhenGuildRestrictionEnabledAndCharacterInGuild()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 502, AccountId = 2000006, Name = "GuildLocked", CharNum = 0, DeleteDate = 0, GuildId = 321
            }
        ]);

        var (service, _) = CreateService(repo, config =>
        {
            config.Char.CharDeleteRestriction = 0x02;
        });

        var response = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 2000006,
            CharacterId = 502
        }, TestServerCallContext.Instance);

        var stored = await repo.GetByIdAsync(502);
        Assert.False(response.Success);
        Assert.NotNull(stored);
        Assert.Equal<uint>(0, stored!.DeleteDate);
    }

    [Fact]
    public async Task DeleteCharacter_ShouldReject_WhenBaseLevelBlockedByConfig()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 503, AccountId = 2000007, Name = "HighLevel", CharNum = 0, DeleteDate = 0, BaseLevel = 80
            }
        ]);

        var (service, _) = CreateService(repo, config =>
        {
            config.Char.CharDeleteLevel = 50;
        });

        var response = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 2000007,
            CharacterId = 503
        }, TestServerCallContext.Instance);

        var stored = await repo.GetByIdAsync(503);
        Assert.False(response.Success);
        Assert.NotNull(stored);
        Assert.Equal<uint>(0, stored!.DeleteDate);
    }

    [Fact]
    public async Task DeleteCharacter_ShouldSoftDelete_WhenAllowed()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 504, AccountId = 2000008, Name = "Deletable", CharNum = 0, DeleteDate = 0, Online = 1, BaseLevel = 10
            }
        ]);

        var (service, _) = CreateService(repo, config =>
        {
            config.Char.CharDeleteRestriction = 0;
            config.Char.CharDeleteLevel = 0;
        });

        var response = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 2000008,
            CharacterId = 504
        }, TestServerCallContext.Instance);

        var stored = await repo.GetByIdAsync(504);
        Assert.True(response.Success);
        Assert.NotNull(stored);
        Assert.True(stored!.DeleteDate > 0);
        Assert.Equal(0, stored.Online);
    }

    [Fact]
    public async Task DeleteCharacter_ShouldRejectAlreadyDeletedCharacter()
    {
        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 702, AccountId = 7010, Name = "AlreadyDeleted", CharNum = 0, DeleteDate = 12345
            }
        ]);

        var (service, _) = CreateService(repo);
        var response = await service.DeleteCharacter(new DeleteCharacterRequest
        {
            AccountId = 7010,
            CharacterId = 702
        }, TestServerCallContext.Instance);

        Assert.False(response.Success);
    }

    [Fact]
    public async Task MapServerRegistryRpcs_ShouldRejectInvalidServerIdsAndCounts()
    {
        var (service, _) = CreateService(new InMemoryCharacterRepository([]));

        var maps = await service.RegisterMapServerMaps(new MapServerMapRegistryRequest
        {
            MapServerId = 0
        }, TestServerCallContext.Instance);
        var users = await service.GetMapServerUserCount(new MapServerUserCountRequest
        {
            MapServerId = 0
        }, TestServerCallContext.Instance);
        var updateUsers = await service.RegisterMapServerUserCount(new MapServerUserCountUpdateRequest
        {
            MapServerId = 0,
            UserCount = -1
        }, TestServerCallContext.Instance);
        var updateAddress = await service.UpdateMapServerAddress(new MapServerAddressUpdateRequest
        {
            MapServerId = 0
        }, TestServerCallContext.Instance);

        Assert.False(maps.Success);
        Assert.False(users.Success);
        Assert.False(updateUsers.Success);
        Assert.False(updateAddress.Success);
    }

    private static (CharGrpcService service, MapAuthTicketService ticketService) CreateService(
        InMemoryCharacterRepository characterRepository,
        Action<CharServerConfiguration>? configure = null)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var config = new CharServerConfiguration();
        configure?.Invoke(config);

        var packetSystem = new PacketSystem();
        var sessionManager = new SessionManager(
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("tests"),
            config);

        var state = new CharServerState();
        state.SetState(ServerState.Running);

        var loginIpc = new LoginServerIpcService(
            new ServerConnectionService(),
            loggerFactory.CreateLogger<LoginServerIpcService>());

        var charServer = new CharServerImpl(
            config,
            loggerFactory.CreateLogger<CharServerImpl>(),
            new ServiceCollection().BuildServiceProvider(),
            packetSystem,
            sessionManager,
            new ServerConnectionService(),
            state,
            loginIpc);

        var ticketService = new MapAuthTicketService();
        var dbContext = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().Options);

        var grpc = new CharGrpcService(
            charServer,
            ticketService,
            new MapServerRegistryService(),
            loginIpc,
            new MapServerIpcService(new ServerConnectionService(), loggerFactory.CreateLogger<MapServerIpcService>()),
            characterRepository,
            new NoOpFriendRepository(),
            dbContext,
            config,
            loggerFactory.CreateLogger<CharGrpcService>());

        return (grpc, ticketService);
    }

    private sealed class InMemoryCharacterRepository(IEnumerable<CharEntity> seed) : ICharacterRepository
    {
        private readonly Dictionary<int, CharEntity> _store = seed.ToDictionary(c => c.CharId, Clone);

        public Task<CharEntity?> GetByIdAsync(int charId, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(charId, out var entity) ? Clone(entity) : null);

        public Task<CharEntity?> GetByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_store.Values.FirstOrDefault(c => c.Name == name) is { } entity ? Clone(entity) : null);

        public Task<IReadOnlyList<CharEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharEntity>>(_store.Values.Where(c => c.AccountId == accountId).Select(Clone).ToList());

        public Task<IReadOnlyList<CharEntity>> GetOnlineCharactersAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharEntity>>(_store.Values.Where(c => c.Online != 0).Select(Clone).ToList());

        public Task<IReadOnlyList<CharEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharEntity>>(_store.Values.Select(Clone).ToList());

        public Task<CharEntity> AddAsync(CharEntity entity, CancellationToken ct = default)
        {
            if (entity.CharId <= 0)
            {
                var nextId = _store.Count == 0 ? 1 : _store.Keys.Max() + 1;
                entity.CharId = nextId;
            }

            _store[entity.CharId] = Clone(entity);
            return Task.FromResult(Clone(entity));
        }

        public Task UpdateAsync(CharEntity entity, CancellationToken ct = default)
        {
            _store[entity.CharId] = Clone(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int charId, CancellationToken ct = default)
        {
            _store.Remove(charId);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int charId, CancellationToken ct = default)
            => Task.FromResult(_store.ContainsKey(charId));

        public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_store.Values.Any(c => c.Name == name));

        private static CharEntity Clone(CharEntity source)
        {
            return new CharEntity
            {
                CharId = source.CharId,
                AccountId = source.AccountId,
                CharNum = source.CharNum,
                Name = source.Name,
                Class = source.Class,
                BaseLevel = source.BaseLevel,
                JobLevel = source.JobLevel,
                LastMap = source.LastMap,
                LastX = source.LastX,
                LastY = source.LastY,
                SaveMap = source.SaveMap,
                SaveX = source.SaveX,
                SaveY = source.SaveY,
                PartyId = source.PartyId,
                GuildId = source.GuildId,
                Online = source.Online,
                DeleteDate = source.DeleteDate,
                LastLogin = source.LastLogin
            };
        }
    }

    private sealed class NoOpFriendRepository : IFriendRepository
    {
        public Task<IReadOnlyList<FriendEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FriendEntity>>([]);

        public Task<FriendEntity> AddAsync(FriendEntity entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task DeleteAsync(int charId, int friendId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> AreFriendsAsync(int charId, int friendId, CancellationToken ct = default)
            => Task.FromResult(false);
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        public static readonly TestServerCallContext Instance = new();

        protected override string MethodCore => "test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => new();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
    }
}
