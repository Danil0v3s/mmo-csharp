using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Party;
using Map.Server.Party;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Party;

/// <summary>
/// GP-PARTY — create + invite-by-name handlers (rAthena clif_parse_CreateParty / PartyInvite2).
/// </summary>
public class PartyCreateInviteHandlersTests
{
    [Fact]
    public async Task Create_calls_char_rpc_stamps_party_id_and_notifies()
    {
        var ctx = New();
        var founder = ctx.AddPc(1, 100, "Alice"); // PartyId 0
        ctx.PartyIpc.CreateResult = new PartyCreateResponse { Success = true, PartyId = 42 };
        var h = new PartyCreateHandler(ctx.Entities, ctx.PartyIpc, ctx.PartyClient, NullLogger<PartyCreateHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(founder), Cz(new CZ_MAKE_GROUP(), ("PartyName", "Heroes")));

        Assert.Equal("Heroes", ctx.PartyIpc.LastCreateName);
        Assert.Equal(42, founder.PartyId);
        Assert.Single(ctx.PartyClient.Created);
        Assert.Equal(42, ctx.PartyClient.Created[0].PartyId);
    }

    [Fact]
    public async Task Create_when_already_in_party_is_noop()
    {
        var ctx = New();
        var founder = ctx.AddPc(1, 100, "Alice");
        founder.PartyId = 5;
        var h = new PartyCreateHandler(ctx.Entities, ctx.PartyIpc, ctx.PartyClient, NullLogger<PartyCreateHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(founder), Cz(new CZ_MAKE_GROUP(), ("PartyName", "Heroes")));

        Assert.Null(ctx.PartyIpc.LastCreateName);   // no RPC
        Assert.Empty(ctx.PartyClient.Created);
    }

    [Fact]
    public async Task Invite_sends_popup_to_online_target()
    {
        var ctx = New();
        var inviter = ctx.AddPc(1, 100, "Alice"); inviter.PartyId = 42;
        var target = ctx.AddPc(2, 200, "Bob");     // PartyId 0
        var h = new PartyInviteHandler(ctx.Entities, ctx.PartyService, ctx.PartyClient, NullLogger<PartyInviteHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(inviter), Cz(new CZ_PARTY_JOIN_REQ(), ("TargetName", "Bob")));

        Assert.Single(ctx.PartyClient.JoinRequests);
        Assert.Equal(target.CharacterId, ctx.PartyClient.JoinRequests[0].Target);
        Assert.Equal(42, ctx.PartyClient.JoinRequests[0].PartyId);
    }

    [Fact]
    public async Task Invite_when_inviter_has_no_party_is_noop()
    {
        var ctx = New();
        var inviter = ctx.AddPc(1, 100, "Alice"); // PartyId 0
        ctx.AddPc(2, 200, "Bob");
        var h = new PartyInviteHandler(ctx.Entities, ctx.PartyService, ctx.PartyClient, NullLogger<PartyInviteHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(inviter), Cz(new CZ_PARTY_JOIN_REQ(), ("TargetName", "Bob")));

        Assert.Empty(ctx.PartyClient.JoinRequests);
    }

    [Fact]
    public async Task Invite_target_not_online_is_noop()
    {
        var ctx = New();
        var inviter = ctx.AddPc(1, 100, "Alice"); inviter.PartyId = 42;
        var h = new PartyInviteHandler(ctx.Entities, ctx.PartyService, ctx.PartyClient, NullLogger<PartyInviteHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(inviter), Cz(new CZ_PARTY_JOIN_REQ(), ("TargetName", "Ghost")));

        Assert.Empty(ctx.PartyClient.JoinRequests);
    }

    [Fact]
    public async Task Invite_target_already_in_party_is_noop()
    {
        var ctx = New();
        var inviter = ctx.AddPc(1, 100, "Alice"); inviter.PartyId = 42;
        var target = ctx.AddPc(2, 200, "Bob"); target.PartyId = 9;
        var h = new PartyInviteHandler(ctx.Entities, ctx.PartyService, ctx.PartyClient, NullLogger<PartyInviteHandler>.Instance);

        await h.HandleAsync(ctx.SessionOf(inviter), Cz(new CZ_PARTY_JOIN_REQ(), ("TargetName", "Bob")));

        Assert.Empty(ctx.PartyClient.JoinRequests);
    }

    // --- helpers ---

    private static T Cz<T>(T packet, params (string prop, object val)[] fields) where T : IncomingPacket
    {
        foreach (var (prop, val) in fields) typeof(T).GetProperty(prop)!.SetValue(packet, val);
        return packet;
    }

    private static Ctx New()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var entities = new EntityRegistry(new StubWorld(map));
        return new Ctx(entities, new InMemorySessions(), new StubPartyClient(), new StubPartyIpc(), new StubPartyService(),
            (uint)"test_map".GetHashCode());
    }

    private sealed record Ctx(EntityRegistry Entities, InMemorySessions Sessions, StubPartyClient PartyClient,
        StubPartyIpc PartyIpc, StubPartyService PartyService, uint MapId)
    {
        public PlayerEntity AddPc(int charId, int accountId, string name)
        {
            var pc = new PlayerEntity(charId, accountId, name, Guid.NewGuid(), MapId, 50, 50) { Hp = 1, MaxHp = 1 };
            Entities.Add(pc);
            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
            { AccountId = accountId, CharacterId = charId, AuthState = MapAuthState.Spawned, EntityId = pc.Id,
              CharacterData = new CharacterDataResponse { MapName = "test_map" } };
            Sessions.Register(pc.Id, accountId, session);
            return pc;
        }
        public MapSessionData SessionOf(PlayerEntity pc) => Sessions.GetByEntityId(pc.Id)!;
    }

    private sealed class StubPartyClient : IPartyClientService
    {
        public List<(int PartyId, string Name)> Created { get; } = new();
        public List<(int Target, int PartyId)> JoinRequests { get; } = new();
        private readonly Dictionary<int, (int, int)> _pending = new();
        public void NotifyPartyCreated(PlayerEntity founder, int partyId, string partyName) => Created.Add((partyId, partyName));
        public void NotifyJoinRequest(PlayerEntity target, PlayerEntity inviter, int partyId, string partyName)
        { JoinRequests.Add((target.CharacterId, partyId)); _pending[target.CharacterId] = (partyId, inviter.CharacterId); }
        public void StashPendingInvite(int t, int p, int i) => _pending[t] = (p, i);
        public (int partyId, int inviterCharacterId)? ConsumePendingInvite(int t)
            => _pending.TryGetValue(t, out var s) ? s : null;
        public void NotifyInviteReply(PlayerEntity inviter, string targetName, int result) { }
        public void NotifyMemberJoined(MapPartyEntity party, MapPartyMember newMember) { }
        public void NotifyMemberWithdraw(MapPartyEntity party, int accountId, string memberName, byte reason) { }
        public void NotifyOptionChanged(MapPartyEntity party, uint expOption, byte itemPickupRule, byte itemShareRule) { }
        public void NotifyDotRemove(MapPartyEntity party, int characterId, int accountId) { }
    }

    private sealed class StubPartyService : IPartyService
    {
        public MapPartyEntity? Get(int partyId) => null;
        public bool IsLeader(PlayerEntity pc) => false;
        public MapPartyMember? GetMember(int partyId, int characterId) => null;
        public int SkillCheck(PlayerEntity caster, int partyId, ushort skillId, ushort skillLevel) => 0;
        public void OnLevelUp(PlayerEntity pc) { }
        public MapPartyEntity ApplySnapshot(int partyId, string name, byte exp, byte item, int leaderCharacterId, int leaderAccountId, IEnumerable<MapPartyMember> members) => null!;
        public void Forget(int partyId) { }
        public void UpdateMemberMap(int partyId, int characterId, string mapName, uint level, bool online) { }
        public void Hydrate(int partyId, int requestingCharacterId) { }
        public Task<MapPartyEntity?> HydrateAsync(int partyId, int requestingCharacterId, CancellationToken ct = default) => Task.FromResult<MapPartyEntity?>(null);
    }

    private sealed class StubPartyIpc : ICharServerIpcServiceParty
    {
        public PartyCreateResponse? CreateResult; public string? LastCreateName;
        public Task<PartyCreateResponse?> PartyCreateAsync(string name, int item, int item2, int leaderAccountId, long leaderCharacterId, string leaderName, int leaderClassId, string leaderMapName, uint leaderLevel, CancellationToken ct = default)
        { LastCreateName = name; return Task.FromResult(CreateResult); }
        public Task<PartyInfoResponse?> PartyInfoAsync(int partyId, long characterId, CancellationToken ct = default) => Task.FromResult<PartyInfoResponse?>(null);
        public Task<PartyAddMemberResponse?> PartyAddMemberAsync(int partyId, int accountId, long characterId, string name, int classId, string mapName, uint level, CancellationToken ct = default) => Task.FromResult<PartyAddMemberResponse?>(null);
        public Task<PartyChangeOptionResponse?> PartyChangeOptionAsync(int partyId, int accountId, int exp, int item, CancellationToken ct = default) => Task.FromResult<PartyChangeOptionResponse?>(null);
        public Task<PartyLeaveResponse?> PartyLeaveAsync(int partyId, int accountId, long characterId, string name, int withdrawType, CancellationToken ct = default) => Task.FromResult<PartyLeaveResponse?>(null);
        public Task<PartyChangeMapResponse?> PartyChangeMapAsync(int partyId, int accountId, long characterId, bool online, uint level, string mapName, CancellationToken ct = default) => Task.FromResult<PartyChangeMapResponse?>(null);
        public Task<PartyBreakResponse?> PartyBreakAsync(int partyId, CancellationToken ct = default) => Task.FromResult<PartyBreakResponse?>(null);
        public Task<PartyMessageResponse?> PartyMessageAsync(int partyId, int accountId, string message, CancellationToken ct = default) => Task.FromResult<PartyMessageResponse?>(null);
        public Task<PartyLeaderChangeResponse?> PartyLeaderChangeAsync(int partyId, int accountId, long characterId, CancellationToken ct = default) => Task.FromResult<PartyLeaderChangeResponse?>(null);
        public Task<PartyShareLevelResponse?> PartyShareLevelAsync(uint shareLevel, CancellationToken ct = default) => Task.FromResult<PartyShareLevelResponse?>(null);
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _byEid = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int accountId, MapSessionData s) { _byEid[id] = s; _byAcc[accountId] = s; }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEid.GetValueOrDefault(entityId);
        public MapSessionData? GetByAccountId(int accountId) => _byAcc.GetValueOrDefault(accountId);
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
