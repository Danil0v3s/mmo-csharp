using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Party;
using Map.Server.Party;
using Map.Server.Services.Intif;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Party;

/// <summary>
/// Wave 92 — coverage for <see cref="PartyJoinReqAckHandler"/>
/// (rAthena <c>clif_parse_ReplyPartyInvite</c> /
/// <c>party_reply_invite</c>). Validates the accept-path dispatches
/// <see cref="IIntifService.AddPartyMember"/> and the reject-path
/// notifies the inviter without dispatching.
/// </summary>
public class PartyJoinReqAckHandlerTests
{
    [Fact]
    public async Task Accept_DispatchesAddMember_AndNotifiesInviter()
    {
        var ctx = New();
        var inviter = ctx.AddPc(100, 1000, "Alice");
        var target = ctx.AddPc(200, 2000, "Bob");
        ctx.PartyClient.StashPendingInvite(target.CharacterId, partyId: 42, inviterCharacterId: inviter.CharacterId);

        var packet = MakeAck(partyId: 42, flag: 1);
        await ctx.Handler.HandleAsync(ctx.SessionOf(target), packet);

        Assert.Equal(1, ctx.Intif.AddPartyMemberCalls);
        Assert.Equal(42, ctx.Intif.LastPartyId);
        Assert.Same(target, ctx.Intif.LastPc);
        Assert.Single(ctx.PartyClient.InviteReplies);
        Assert.Equal(2, ctx.PartyClient.InviteReplies[0].Result); // PARTY_REPLY_ACCEPTED
    }

    [Fact]
    public async Task Reject_NotifiesInviter_WithoutDispatch()
    {
        var ctx = New();
        var inviter = ctx.AddPc(100, 1000, "Alice");
        var target = ctx.AddPc(200, 2000, "Bob");
        ctx.PartyClient.StashPendingInvite(target.CharacterId, partyId: 42, inviterCharacterId: inviter.CharacterId);

        var packet = MakeAck(partyId: 42, flag: 0);
        await ctx.Handler.HandleAsync(ctx.SessionOf(target), packet);

        Assert.Equal(0, ctx.Intif.AddPartyMemberCalls);
        Assert.Single(ctx.PartyClient.InviteReplies);
        Assert.Equal(1, ctx.PartyClient.InviteReplies[0].Result); // PARTY_REPLY_REJECTED
    }

    [Fact]
    public async Task NoPendingInvite_IsNoOp()
    {
        var ctx = New();
        var target = ctx.AddPc(200, 2000, "Bob");

        var packet = MakeAck(partyId: 42, flag: 1);
        await ctx.Handler.HandleAsync(ctx.SessionOf(target), packet);

        Assert.Equal(0, ctx.Intif.AddPartyMemberCalls);
        Assert.Empty(ctx.PartyClient.InviteReplies);
    }

    [Fact]
    public async Task PartyIdMismatch_DropsTheAck()
    {
        var ctx = New();
        var inviter = ctx.AddPc(100, 1000, "Alice");
        var target = ctx.AddPc(200, 2000, "Bob");
        ctx.PartyClient.StashPendingInvite(target.CharacterId, partyId: 42, inviterCharacterId: inviter.CharacterId);

        // Client replies with a stale party id (different from the pending invite).
        var packet = MakeAck(partyId: 99, flag: 1);
        await ctx.Handler.HandleAsync(ctx.SessionOf(target), packet);

        Assert.Equal(0, ctx.Intif.AddPartyMemberCalls);
        // The mismatched ack drains the pending slot but doesn't emit reply.
        Assert.Empty(ctx.PartyClient.InviteReplies);
    }

    private static CZ_PARTY_JOIN_REQ_ACK MakeAck(uint partyId, byte flag)
    {
        var p = new CZ_PARTY_JOIN_REQ_ACK();
        typeof(CZ_PARTY_JOIN_REQ_ACK).GetProperty("PartyId")!.SetValue(p, partyId);
        typeof(CZ_PARTY_JOIN_REQ_ACK).GetProperty("Flag")!.SetValue(p, flag);
        return p;
    }

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var sessions = new InMemorySessions();
        var partyClient = new StubPartyClient();
        var intif = new StubIntif();
        var partyService = new StubPartyService();
        var handler = new PartyJoinReqAckHandler(entities, partyClient, intif,
            NullLogger<PartyJoinReqAckHandler>.Instance);
        return new TestContext(handler, entities, sessions, partyClient, intif, partyService, (uint)mapName.GetHashCode());
    }

    // --- GP-PARTY manage handlers (leave / expel / change-leader / change-option) ---

    [Fact]
    public async Task Leave_dispatches_and_clears_party_id()
    {
        var ctx = New();
        var pc = ctx.AddPc(1, 100, "Alice"); pc.PartyId = 42;
        var h = new PartyLeaveHandler(ctx.Entities, ctx.Intif, NullLogger<PartyLeaveHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(pc), new CZ_REQ_LEAVE_GROUP());

        Assert.Equal((42, 100, 1), ctx.Intif.LastLeave);
        Assert.Equal(0, pc.PartyId);
    }

    [Fact]
    public async Task Expel_by_leader_removes_target()
    {
        var ctx = New();
        var leader = ctx.AddPc(1, 100, "Alice"); leader.PartyId = 42;
        var bob = ctx.AddPc(2, 200, "Bob"); bob.PartyId = 42;
        ctx.PartyService.SetParty(42, leaderCharId: 1,
            (acc: 100, ch: 1, "Alice"), (acc: 200, ch: 2, "Bob"));
        var h = new PartyExpelHandler(ctx.Entities, ctx.PartyService, ctx.Intif, NullLogger<PartyExpelHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(leader), Expel(accountId: 200, "Bob"));

        Assert.Equal((42, 200, 2), ctx.Intif.LastLeave);
        Assert.Equal(0, bob.PartyId); // online target cleared locally
    }

    [Fact]
    public async Task Expel_by_non_leader_is_noop()
    {
        var ctx = New();
        var member = ctx.AddPc(2, 200, "Bob"); member.PartyId = 42;
        ctx.AddPc(1, 100, "Alice");
        ctx.PartyService.SetParty(42, leaderCharId: 1, (100, 1, "Alice"), (200, 2, "Bob"));
        var h = new PartyExpelHandler(ctx.Entities, ctx.PartyService, ctx.Intif, NullLogger<PartyExpelHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(member), Expel(accountId: 100, "Alice")); // Bob isn't leader

        Assert.Equal(0, ctx.Intif.LeavePartyCalls);
    }

    [Fact]
    public async Task ChangeLeader_by_leader_dispatches()
    {
        var ctx = New();
        var leader = ctx.AddPc(1, 100, "Alice"); leader.PartyId = 42;
        ctx.PartyService.SetParty(42, leaderCharId: 1, (100, 1, "Alice"), (200, 2, "Bob"));
        var h = new PartyChangeLeaderHandler(ctx.Entities, ctx.PartyService, ctx.Intif, NullLogger<PartyChangeLeaderHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(leader), ChangeLeader(accountId: 200));

        Assert.Equal((42, 200, 2), ctx.Intif.LastLeader);
    }

    [Fact]
    public async Task ChangeOption_by_leader_dispatches_keeping_item()
    {
        var ctx = New();
        var leader = ctx.AddPc(1, 100, "Alice"); leader.PartyId = 42;
        ctx.PartyService.SetParty(42, leaderCharId: 1, item: 2, (100, 1, "Alice"));
        var h = new PartyChangeOptionHandler(ctx.Entities, ctx.PartyService, ctx.Intif, NullLogger<PartyChangeOptionHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(leader), ChangeOption(exp: 1));

        Assert.Equal((42, 100, 1, 2), ctx.Intif.LastOption); // exp=1, item kept = 2
    }

    private static CZ_REQ_EXPEL_GROUP_MEMBER Expel(int accountId, string name)
    {
        var p = new CZ_REQ_EXPEL_GROUP_MEMBER();
        typeof(CZ_REQ_EXPEL_GROUP_MEMBER).GetProperty("AccountId")!.SetValue(p, accountId);
        typeof(CZ_REQ_EXPEL_GROUP_MEMBER).GetProperty("Name")!.SetValue(p, name);
        return p;
    }

    private static CZ_PARTY_CHANGE_LEADER ChangeLeader(int accountId)
    {
        var p = new CZ_PARTY_CHANGE_LEADER();
        typeof(CZ_PARTY_CHANGE_LEADER).GetProperty("AccountId")!.SetValue(p, accountId);
        return p;
    }

    private static CZ_PARTY_CHANGE_OPTION ChangeOption(int exp)
    {
        var p = new CZ_PARTY_CHANGE_OPTION();
        typeof(CZ_PARTY_CHANGE_OPTION).GetProperty("ExpFlag")!.SetValue(p, exp);
        return p;
    }

    private sealed record TestContext(
        PartyJoinReqAckHandler Handler,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        StubPartyClient PartyClient,
        StubIntif Intif,
        StubPartyService PartyService,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, int accountId, string name)
        {
            var pc = new PlayerEntity(charId, accountId, name, Guid.NewGuid(), MapId, 50, 50);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);

            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide, 30000,
                new PacketSystem().Factory, new PacketSystem().Registry,
                NullLogger.Instance)
            {
                AccountId = accountId,
                CharacterId = charId,
                AuthState = MapAuthState.Spawned,
                EntityId = pc.Id,
            };
            Sessions.Register(pc.Id, accountId, session);
            return pc;
        }

        public MapSessionData SessionOf(PlayerEntity pc) => Sessions.GetByEntityId(pc.Id)!;
    }

    private sealed class StubPartyClient : IPartyClientService
    {
        private readonly Dictionary<int, (int Party, int Inviter)> _pending = new();
        public List<(PlayerEntity Inviter, string Target, int Result)> InviteReplies { get; } = new();

        public void StashPendingInvite(int targetCharacterId, int partyId, int inviterCharacterId)
            => _pending[targetCharacterId] = (partyId, inviterCharacterId);
        public (int partyId, int inviterCharacterId)? ConsumePendingInvite(int targetCharacterId)
        {
            if (_pending.TryGetValue(targetCharacterId, out var slot))
            {
                _pending.Remove(targetCharacterId);
                return (slot.Party, slot.Inviter);
            }
            return null;
        }
        public void NotifyPartyCreated(PlayerEntity founder, int partyId, string partyName) { }
        public void NotifyJoinRequest(PlayerEntity target, PlayerEntity inviter, int partyId, string partyName)
            => StashPendingInvite(target.CharacterId, partyId, inviter.CharacterId);
        public void NotifyInviteReply(PlayerEntity inviter, string targetName, int result)
            => InviteReplies.Add((inviter, targetName, result));
        public void NotifyMemberJoined(MapPartyEntity party, MapPartyMember newMember) { }
        public void NotifyMemberWithdraw(MapPartyEntity party, int accountId, string memberName, byte reason) { }
        public void NotifyOptionChanged(MapPartyEntity party, uint expOption, byte itemPickupRule, byte itemShareRule) { }
        public void NotifyDotRemove(MapPartyEntity party, int characterId, int accountId) { }
    }

    private sealed class StubIntif : IIntifService
    {
        public int AddPartyMemberCalls;
        public int LastPartyId;
        public PlayerEntity? LastPc;

        public int AddPartyMember(int partyId, PlayerEntity pc)
        {
            AddPartyMemberCalls++;
            LastPartyId = partyId;
            LastPc = pc;
            return 1;
        }

        // Stubs for the rest of the (large) interface; only AddPartyMember is exercised.
        public int RequestChatName(int charId) => 0;
        public int RequestAccInfo(int callerCharId, int targetCharId) => 0;
        public int Broadcast(string mes, uint colorRgb, byte type) => 0;
        public int Broadcast2(string mes, uint colorRgb, ushort fontType, ushort fontSize, ushort fontAlign, ushort fontY) => 0;
        public int MainMessage(PlayerEntity pc, string text) => 0;
        public int WisMessage(PlayerEntity from, string toName, string text) => 0;
        public int WisMessageToGm(string from, byte minGmLevel, string text) => 0;
        public int SaveRegistry(PlayerEntity pc) => 0;
        public int RequestRegistry(PlayerEntity pc, byte flag) => 0;
        public int CreateParty(PlayerEntity pc, string name, byte item, byte itemDiv) => 0;
        public int RequestPartyInfo(int partyId, int charId) => 0;
        public int ChangePartyLeader(int partyId, int accountId, int charId) { LastLeader = (partyId, accountId, charId); return 1; }
        public int PartyChangeOption(int partyId, int accountId, int exp, int item, int flag) { LastOption = (partyId, accountId, exp, item); return 1; }
        public int LeavePartyCalls; public (int, int, int) LastLeave;
        public (int, int, int)? LastLeader; public (int, int, int, int)? LastOption;
        public int LeaveParty(int partyId, int accountId, int charId) { LeavePartyCalls++; LastLeave = (partyId, accountId, charId); return 1; }
        public int PartyChangemap(PlayerEntity pc, bool online) => 0;
        public int BreakParty(int partyId) => 0;
        public int PartyMessage(int partyId, int accountId, string text) => 0;
        public bool GuildCreate(string name, PlayerEntity master) => false;
        public int GuildRequestInfo(int guildId) => 0;
        public int GuildAddMember(int guildId, PlayerEntity pc) => 0;
        public int GuildLeave(int guildId, int accountId, int charId, byte flag, string mes) => 0;
        public int GuildExpulsion(int guildId, int accountId, int charId, string mes) => 0;
        public int GuildBreak(int guildId) => 0;
        public int GuildMessage(int guildId, int accountId, string mes) => 0;
        public int GuildEmblem(int guildId, byte[] emblem) => 0;
        public int GuildSavePosition(int guildId, byte idx, int mode, int exp_mode, string name) => 0;
        public int GuildSetSkill(int guildId, ushort skillId, ushort skillLevel) => 0;
        public int GuildAllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes) => 0;
        public int GuildAddCastle(int castleId, int guildId) => 0;
        public int MailRequestInbox(int charId, byte flag) => 0;
        public int MailRead(int mailId) => 0;
        public int MailGetAttach(int charId, int mailId, byte flag) => 0;
        public int MailDelete(int charId, int mailId) => 0;
        public int MailSend(int senderCharId, string toName, string title, string body, int zeny) => 0;
        public int MailReturn(int charId, int mailId) => 0;
        public int AuctionRequestList(int charId, byte type, int price, string search, byte page) => 0;
        public int AuctionRegister(int charId, byte type, int sellerCharId, string sellerName, int now, int hours, int priceStart, int priceBuyNow, int itemId, byte refine, byte attribute, int identify, int amount) => 0;
        public int AuctionCancel(int charId, uint auctionId) => 0;
        public int AuctionClose(int charId, uint auctionId) => 0;
        public int AuctionBid(int charId, uint auctionId, int bid, string bidder) => 0;
        public int QuestSave(PlayerEntity pc) => 0;
        public int QuestRequest(int charId) => 0;
        public System.Threading.Tasks.Task QuestRequestAsync(Map.Server.Entities.PlayerEntity pc, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public int AchievementSave(PlayerEntity pc) => 0;
        public int AchievementRequest(int charId) => 0;
        public System.Threading.Tasks.Task QuestSaveAsync(PlayerEntity pc, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task AchievementSaveAsync(PlayerEntity pc, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task SavePetAsync(int petId, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName) => 0;
        public System.Threading.Tasks.Task<int> PetCreateAsync(PlayerEntity master, int classId, int eggItemId, byte intimate, byte hungry, string petName, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult(0);
        public System.Threading.Tasks.Task<Core.Server.IPC.PetData?> PetLoadAsync(int petId, int accountId, int charId, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult<Core.Server.IPC.PetData?>(null);
        public int RequestPetInfo(int petId, int accountId, byte flag) => 0;
        public int SavePet(int petId) => 0;
        public int DeletePet(int petId) => 0;
        public int HomunculusCreate(int accountId, byte[] data) => 0;
        public int HomunculusRequest(int accountId, int homunId) => 0;
        public int HomunculusSave(byte[] data) => 0;
        public int HomunculusDelete(int homunId) => 0;
        public int MercenaryCreate(byte[] data) => 0;
        public int MercenaryRequest(int accountId, int mercId) => 0;
        public int MercenarySave(byte[] data) => 0;
        public int MercenaryDelete(int mercId) => 0;
        public int ClanMessage(int clanId, int charId, string text) => 0;
        public int RequestAccountStorage(int accountId) => 0;
        public int SaveAccountStorage(int accountId, byte[] data) => 0;
        public int RequestGuildStorage(int charId, int guildId) => 0;
        public int SaveGuildStorage(int charId, int guildId, byte[] data) => 0;
        public int CreateBg(int mapIndex, byte side) => 0;
        public int BgRecord(int bgId, int charId, byte score) => 0;
        public int ElementalCreate(byte[] data) => 0;
        public int ElementalRequest(int accountId, int eleId) => 0;
        public int ElementalSave(byte[] data) => 0;
        public int ElementalDelete(int eleId) => 0;
        public int RequestMapreg() => 0;
        public int SaveMapreg(byte[] data) => 0;
        public bool CheckConnection() => true;
        public void Init() { }
        public void Final() { }
    }

    private sealed class StubPartyService : IPartyService
    {
        private MapPartyEntity? _party; private int _leaderCharId;
        public void SetParty(int partyId, int leaderCharId, params (int acc, int ch, string name)[] members)
            => SetParty(partyId, leaderCharId, item: 0, members);
        public void SetParty(int partyId, int leaderCharId, int item, params (int acc, int ch, string name)[] members)
        {
            _leaderCharId = leaderCharId;
            _party = new MapPartyEntity(partyId) { LeaderCharacterId = leaderCharId, Item = (byte)item };
            foreach (var (acc, ch, name) in members)
                _party.Members[ch] = new MapPartyMember { AccountId = acc, CharacterId = ch, Name = name, Leader = ch == leaderCharId };
        }
        public MapPartyEntity? Get(int partyId) => _party;
        public bool IsLeader(PlayerEntity pc) => _party != null && pc.CharacterId == _leaderCharId;
        public MapPartyMember? GetMember(int partyId, int characterId) => _party?.Members.GetValueOrDefault(characterId);
        public int SkillCheck(PlayerEntity caster, int partyId, ushort skillId, ushort skillLevel) => 0;
        public void OnLevelUp(PlayerEntity pc) { }
        public MapPartyEntity ApplySnapshot(int partyId, string name, byte exp, byte item, int leaderCharacterId, int leaderAccountId, IEnumerable<MapPartyMember> members) => _party!;
        public void Forget(int partyId) { }
        public void UpdateMemberMap(int partyId, int characterId, string mapName, uint level, bool online) { }
        public void Hydrate(int partyId, int requestingCharacterId) { }
        public Task<MapPartyEntity?> HydrateAsync(int partyId, int requestingCharacterId, CancellationToken ct = default) => Task.FromResult(_party);
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _byEid = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int accountId, MapSessionData s)
        {
            _byEid[id] = s;
            _byAcc[accountId] = s;
        }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEid.GetValueOrDefault(entityId);
        public MapSessionData? GetByAccountId(int accountId) => _byAcc.GetValueOrDefault(accountId);
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
