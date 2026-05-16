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

/// <summary>
/// P8 cascade & persistence tests: verify each module's gRPC surface preserves
/// rAthena semantics that map-server gameplay will depend on. Each test seeds
/// realistic state via the DbContext and exercises the gRPC method, then asserts
/// the DB row state after the call.
/// </summary>
public class CharGrpcModuleCascadeTests
{
    // --- Party ---

    [Fact]
    public async Task PartyLeave_WhenLeaderLeaves_DisbandsParty()
    {
        var (service, db) = CreateService();

        // Seed: party with leader (acc 100, char 1) and one member (acc 101, char 2).
        db.Parties.Add(new PartyEntity
        {
            PartyId = 50, Name = "Test", LeaderId = 100, LeaderChar = 1
        });
        db.Characters.Add(new CharEntity { CharId = 1, AccountId = 100, Name = "Leader", PartyId = 50, DeleteDate = 0 });
        db.Characters.Add(new CharEntity { CharId = 2, AccountId = 101, Name = "Member", PartyId = 50, DeleteDate = 0 });
        await db.SaveChangesAsync();

        var response = await service.PartyLeave(new PartyLeaveRequest
        {
            PartyId = 50, CharacterId = 1, AccountId = 100
        }, TestServerCallContext.Instance);

        Assert.True(response.Success);

        // Party row deleted; both members' party_id cleared.
        Assert.False(await db.Parties.AnyAsync(p => p.PartyId == 50));
        Assert.Equal(0, (await db.Characters.AsNoTracking().FirstAsync(c => c.CharId == 1)).PartyId);
        Assert.Equal(0, (await db.Characters.AsNoTracking().FirstAsync(c => c.CharId == 2)).PartyId);
    }

    [Fact]
    public async Task PartyLeave_WhenNonLeaderLeaves_KeepsParty()
    {
        var (service, db) = CreateService();
        db.Parties.Add(new PartyEntity { PartyId = 51, Name = "Test", LeaderId = 100, LeaderChar = 1 });
        db.Characters.Add(new CharEntity { CharId = 1, AccountId = 100, Name = "Leader", PartyId = 51, DeleteDate = 0 });
        db.Characters.Add(new CharEntity { CharId = 2, AccountId = 101, Name = "Member", PartyId = 51, DeleteDate = 0 });
        await db.SaveChangesAsync();

        await service.PartyLeave(new PartyLeaveRequest
        {
            PartyId = 51, CharacterId = 2, AccountId = 101
        }, TestServerCallContext.Instance);

        Assert.True(await db.Parties.AnyAsync(p => p.PartyId == 51));
        Assert.Equal(51, (await db.Characters.AsNoTracking().FirstAsync(c => c.CharId == 1)).PartyId);
        Assert.Equal(0, (await db.Characters.AsNoTracking().FirstAsync(c => c.CharId == 2)).PartyId);
    }

    // --- Guild ---

    [Fact]
    public async Task GuildBreak_CleansAllRelatedRows()
    {
        var (service, db) = CreateService();

        // Seed: guild + 2 members + position + skill + alliance + expulsion + castle + storage.
        db.Guilds.Add(new GuildEntity { GuildId = 200, Name = "Test", CharId = 1, Master = "Boss", GuildLv = 1, MaxMember = 16 });
        db.Characters.Add(new CharEntity { CharId = 1, AccountId = 100, Name = "Boss", GuildId = 200, DeleteDate = 0 });
        db.Characters.Add(new CharEntity { CharId = 2, AccountId = 101, Name = "Grunt", GuildId = 200, DeleteDate = 0 });
        db.GuildMembers.Add(new GuildMemberEntity { GuildId = 200, CharId = 1, Position = 0 });
        db.GuildMembers.Add(new GuildMemberEntity { GuildId = 200, CharId = 2, Position = 1 });
        db.GuildPositions.Add(new GuildPositionEntity { GuildId = 200, Position = 0, Name = "Master" });
        db.GuildSkills.Add(new GuildSkillEntity { GuildId = 200, Id = 10000, Lv = 3 });
        db.GuildAlliances.Add(new GuildAllianceEntity { GuildId = 200, AllianceId = 201, Opposition = 0, Name = "Other" });
        db.GuildExpulsions.Add(new GuildExpulsionEntity { GuildId = 200, AccountId = 99, Name = "Kicked", Mes = "bye" });
        db.GuildCastles.Add(new GuildCastleEntity { CastleId = 1, GuildId = 200 });
        db.GuildStoragePayloads.Add(new GuildStoragePayloadEntity { GuildId = 200, Data = new byte[] { 1, 2, 3 } });
        await db.SaveChangesAsync();

        var response = await service.GuildBreak(new GuildBreakRequest
        {
            GuildId = 200
        }, TestServerCallContext.Instance);

        Assert.True(response.Success);

        // Guild + all owned rows removed; member char.guild_id cleared; castle guild_id cleared (not deleted).
        Assert.False(await db.Guilds.AnyAsync(g => g.GuildId == 200));
        Assert.False(await db.GuildMembers.AnyAsync(m => m.GuildId == 200));
        Assert.False(await db.GuildPositions.AnyAsync(p => p.GuildId == 200));
        Assert.False(await db.GuildSkills.AnyAsync(s => s.GuildId == 200));
        Assert.False(await db.GuildAlliances.AnyAsync(a => a.GuildId == 200 || a.AllianceId == 200));
        Assert.False(await db.GuildExpulsions.AnyAsync(e => e.GuildId == 200));
        Assert.False(await db.GuildStoragePayloads.AnyAsync(s => s.GuildId == 200));
        // Castle still exists but with guild_id = 0 (free to be re-captured).
        var castle = await db.GuildCastles.AsNoTracking().FirstAsync(c => c.CastleId == 1);
        Assert.Equal(0, castle.GuildId);
        // Member chars: guild_id cleared.
        Assert.Equal(0, (await db.Characters.AsNoTracking().FirstAsync(c => c.CharId == 1)).GuildId);
        Assert.Equal(0, (await db.Characters.AsNoTracking().FirstAsync(c => c.CharId == 2)).GuildId);
    }

    // --- Mercenary ---

    [Fact]
    public async Task MercenarySave_PersistsSkillCooldowns()
    {
        var (service, db) = CreateService();

        var save = await service.MercenarySave(new MercenarySaveRequest
        {
            Mercenary = new MercenaryData
            {
                MercenaryId = 300, CharacterId = 1, ClassId = 6017,
                Hp = 1000, Sp = 200, KillCount = 5, LifeTime = 3600,
                Cooldowns =
                {
                    new MercenarySkillCooldown { Skill = 8201, Tick = 1500 },
                    new MercenarySkillCooldown { Skill = 8202, Tick = 2500 },
                    new MercenarySkillCooldown { Skill = 0, Tick = 100 } // zero-skill skipped
                }
            }
        }, TestServerCallContext.Instance);

        Assert.True(save.Success);

        var cooldowns = await db.SkillCooldownMercenaries.AsNoTracking()
            .Where(s => s.MerId == 300).ToListAsync();
        Assert.Equal(2, cooldowns.Count);
        Assert.Contains(cooldowns, c => c.Skill == 8201 && c.Tick == 1500);
        Assert.Contains(cooldowns, c => c.Skill == 8202 && c.Tick == 2500);
    }

    [Fact]
    public async Task MercenaryDelete_CascadesToCooldownsAndOwner()
    {
        var (service, db) = CreateService();

        db.Mercenaries.Add(new MercenaryEntity { MerId = 301, CharId = 50, Class = 6017, Hp = 100, Sp = 50 });
        db.MercenaryOwners.Add(new MercenaryOwnerEntity { CharId = 50, MercId = 301 });
        db.SkillCooldownMercenaries.Add(new SkillCooldownMercenaryEntity { MerId = 301, Skill = 8201, Tick = 1000 });
        db.SkillCooldownMercenaries.Add(new SkillCooldownMercenaryEntity { MerId = 301, Skill = 8202, Tick = 2000 });
        await db.SaveChangesAsync();

        var response = await service.MercenaryDelete(new MercenaryDeleteRequest
        {
            MercenaryId = 301
        }, TestServerCallContext.Instance);

        Assert.True(response.Success);
        Assert.False(await db.Mercenaries.AnyAsync(m => m.MerId == 301));
        Assert.False(await db.SkillCooldownMercenaries.AnyAsync(s => s.MerId == 301));
        Assert.False(await db.MercenaryOwners.AnyAsync(o => o.CharId == 50));
    }

    // --- Elemental ---

    [Fact]
    public async Task ElementalCreate_LoadSave_Delete_RoundTrip()
    {
        var (service, db) = CreateService();

        var create = await service.ElementalCreate(new ElementalCreateRequest
        {
            Elemental = new ElementalData
            {
                CharacterId = 60, ClassId = 2114,
                Hp = 500, MaxHp = 500, Sp = 100, MaxSp = 100,
                LifeTime = 120
            }
        }, TestServerCallContext.Instance);
        Assert.True(create.Success);
        var elemId = create.Elemental.ElementalId;
        Assert.True(elemId > 0);

        var load = await service.ElementalLoad(new ElementalLoadRequest
        {
            ElementalId = elemId, CharacterId = 60
        }, TestServerCallContext.Instance);
        Assert.True(load.Success);
        Assert.Equal(2114, load.Elemental.ClassId);

        var save = await service.ElementalSave(new ElementalSaveRequest
        {
            Elemental = new ElementalData
            {
                ElementalId = elemId, CharacterId = 60, ClassId = 2114,
                Hp = 200, MaxHp = 500, Sp = 50, MaxSp = 100, LifeTime = 60
            }
        }, TestServerCallContext.Instance);
        Assert.True(save.Success);
        var stored = await db.Elementals.AsNoTracking().FirstAsync(e => e.EleId == elemId);
        Assert.Equal(200u, stored.Hp);

        var del = await service.ElementalDelete(new ElementalDeleteRequest
        {
            ElementalId = elemId
        }, TestServerCallContext.Instance);
        Assert.True(del.Success);
        Assert.False(await db.Elementals.AnyAsync(e => e.EleId == elemId));
    }

    // --- Achievement ---

    [Fact]
    public async Task Achievement_SaveLoadReward_RoundTrip()
    {
        var (service, db) = CreateService();
        var charId = 70L;

        var save = await service.AchievementSave(new AchievementSaveRequest
        {
            CharacterId = charId,
            Achievements =
            {
                new AchievementEntryData
                {
                    AchievementId = 1, Counts = { 5 }, CompletedUnix = 1000, RewardedUnix = 0
                },
                new AchievementEntryData
                {
                    AchievementId = 2, Counts = { 3 }, CompletedUnix = 0, RewardedUnix = 0
                }
            }
        }, TestServerCallContext.Instance);
        Assert.True(save.Success);

        var load = await service.AchievementLoad(new AchievementLoadRequest
        {
            CharacterId = charId
        }, TestServerCallContext.Instance);
        Assert.True(load.Success);
        Assert.Equal(2, load.Achievements.Count);

        var reward = await service.AchievementReward(new AchievementRewardRequest
        {
            CharacterId = charId, AchievementId = 1
        }, TestServerCallContext.Instance);
        Assert.True(reward.Success);

        var rewarded = await db.Achievements.AsNoTracking()
            .FirstAsync(a => a.CharId == charId && a.Id == 1);
        Assert.NotNull(rewarded.Rewarded);
    }

    // --- Clan ---

    [Fact]
    public async Task Clan_MemberJoinLeft_AdjustsConnectMember()
    {
        var (service, db) = CreateService();
        db.Clans.Add(new ClanEntity { ClanId = 400, Name = "TestClan", ConnectMember = 3, MaxMember = 50 });
        await db.SaveChangesAsync();

        await service.ClanMemberJoined(new ClanMemberStateRequest
        {
            ClanId = 400
        }, TestServerCallContext.Instance);
        Assert.Equal(4, (await db.Clans.AsNoTracking().FirstAsync(c => c.ClanId == 400)).ConnectMember);

        await service.ClanMemberLeft(new ClanMemberStateRequest
        {
            ClanId = 400
        }, TestServerCallContext.Instance);
        Assert.Equal(3, (await db.Clans.AsNoTracking().FirstAsync(c => c.ClanId == 400)).ConnectMember);
    }

    // --- Test infrastructure ---

    private static (CharGrpcService service, GameDbContext db) CreateService()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var config = new CharServerConfiguration();

        var packetSystem = new PacketSystem();
        var sessionManager = new SessionManager(
            packetSystem.Factory, packetSystem.Registry,
            loggerFactory.CreateLogger("tests"), config);

        var state = new CharServerState();
        state.SetState(ServerState.Running);

        var loginIpc = new LoginServerIpcService(
            new ServerConnectionService(),
            loggerFactory.CreateLogger<LoginServerIpcService>());

        var charServer = new CharServerImpl(
            config,
            loggerFactory.CreateLogger<CharServerImpl>(),
            new ServiceCollection().BuildServiceProvider(),
            packetSystem, sessionManager,
            new ServerConnectionService(), state, loginIpc);

        var dbContext = new GameDbContext(
            new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var grpc = new CharGrpcService(
            charServer,
            new MapAuthTicketService(),
            new MapServerRegistryService(),
            loginIpc,
            new MapServerIpcService(new ServerConnectionService(), loggerFactory.CreateLogger<MapServerIpcService>()),
            new CharRepoStub(dbContext),
            new FriendRepoStub(),
            dbContext, config,
            loggerFactory.CreateLogger<CharGrpcService>());

        return (grpc, dbContext);
    }

    private sealed class FriendRepoStub : IFriendRepository
    {
        public Task<IReadOnlyList<FriendEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FriendEntity>>(Array.Empty<FriendEntity>());
        public Task<FriendEntity> AddAsync(FriendEntity entity, CancellationToken ct = default) => Task.FromResult(entity);
        public Task DeleteAsync(int charId, int friendId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> AreFriendsAsync(int charId, int friendId, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class CharRepoStub(GameDbContext db) : ICharacterRepository
    {
        public async Task<CharEntity?> GetByIdAsync(int charId, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.CharId == charId, ct);
        public async Task<CharEntity?> GetByNameAsync(string name, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name, ct);
        public async Task<IReadOnlyList<CharEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().Where(c => c.AccountId == accountId).ToListAsync(ct);
        public async Task<IReadOnlyList<CharEntity>> GetOnlineCharactersAsync(CancellationToken ct = default)
            => await db.Characters.AsNoTracking().Where(c => c.Online != 0).ToListAsync(ct);
        public async Task<IReadOnlyList<CharEntity>> GetAllAsync(CancellationToken ct = default)
            => await db.Characters.AsNoTracking().ToListAsync(ct);
        public async Task<CharEntity> AddAsync(CharEntity entity, CancellationToken ct = default)
        { db.Characters.Add(entity); await db.SaveChangesAsync(ct); return entity; }
        public Task UpdateAsync(CharEntity entity, CancellationToken ct = default) { db.Characters.Update(entity); return db.SaveChangesAsync(ct); }
        public async Task DeleteAsync(int charId, CancellationToken ct = default)
        { var c = await db.Characters.FirstOrDefaultAsync(x => x.CharId == charId, ct); if (c is not null) { db.Characters.Remove(c); await db.SaveChangesAsync(ct); } }
        public async Task<bool> ExistsAsync(int charId, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().AnyAsync(c => c.CharId == charId, ct);
        public async Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().AnyAsync(c => c.Name == name, ct);
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
