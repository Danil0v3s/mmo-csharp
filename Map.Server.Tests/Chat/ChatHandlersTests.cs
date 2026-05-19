using System.Collections.Concurrent;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Chat;
using Map.Server.Entities;
using Map.Server.Handlers.Chat;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Chat;

/// <summary>
/// Wire-level coverage for whisper / party / guild chat handlers.
/// Asserts on the outbound packet ids queued to recipients and the
/// narrow <see cref="IChatIpcOutbound"/> call counts to confirm
/// cross-server hand-off.
/// </summary>
public class ChatHandlersTests
{
    [Fact]
    public async Task Whisper_LocalTarget_DeliversZcWhisper()
    {
        var ctx = New();
        var a = ctx.AddPc(charId: 100, accountId: 1000, name: "Alice");
        var b = ctx.AddPc(charId: 200, accountId: 2000, name: "Bob");
        var aSess = ctx.SessionOf(a);
        var bSess = ctx.SessionOf(b);
        ClearOutbound(aSess); ClearOutbound(bSess);

        await ctx.WhisperHandler.HandleAsync(aSess, MakeWhisper("Bob", "hello"));

        AssertSent(bSess, (ushort)PacketHeader.ZC_WHISPER);
        Assert.Equal(0, ctx.Ipc.WhisperCalls);
    }

    [Fact]
    public async Task Whisper_UnknownTarget_HandsOffToIpc()
    {
        var ctx = New();
        var a = ctx.AddPc(100, 1000, "Alice");
        var aSess = ctx.SessionOf(a);
        ClearOutbound(aSess);

        await ctx.WhisperHandler.HandleAsync(aSess, MakeWhisper("Charlie", "hi"));

        Assert.Equal(1, ctx.Ipc.WhisperCalls);
        Assert.Equal("Charlie", ctx.Ipc.LastWhisperTarget);
    }

    [Fact]
    public async Task PartyChat_FansOutToLocalMembers_AndPostsToIpc()
    {
        var ctx = New();
        var a = ctx.AddPc(100, 1000, "Alice"); a.PartyId = 42;
        var b = ctx.AddPc(200, 2000, "Bob"); b.PartyId = 42;
        var c = ctx.AddPc(300, 3000, "Carol"); c.PartyId = 99;
        var aSess = ctx.SessionOf(a); var bSess = ctx.SessionOf(b); var cSess = ctx.SessionOf(c);
        ClearOutbound(aSess); ClearOutbound(bSess); ClearOutbound(cSess);

        await ctx.PartyHandler.HandleAsync(aSess, MakeParty("Alice : hey team"));

        AssertSent(aSess, (ushort)PacketHeader.ZC_NOTIFY_CHAT_PARTY);
        AssertSent(bSess, (ushort)PacketHeader.ZC_NOTIFY_CHAT_PARTY);
        AssertNotSent(cSess, (ushort)PacketHeader.ZC_NOTIFY_CHAT_PARTY);
        Assert.Equal(1, ctx.Ipc.PartyCalls);
        Assert.Equal(42, ctx.Ipc.LastPartyId);
    }

    [Fact]
    public async Task PartyChat_NoParty_IsNoOp()
    {
        var ctx = New();
        var a = ctx.AddPc(100, 1000, "Alice");
        var aSess = ctx.SessionOf(a);
        ClearOutbound(aSess);

        await ctx.PartyHandler.HandleAsync(aSess, MakeParty("Alice : hello?"));

        Assert.Empty(OutboundQueue(aSess));
        Assert.Equal(0, ctx.Ipc.PartyCalls);
    }

    [Fact]
    public async Task GuildChat_FansOutToLocalGuildMembers()
    {
        var ctx = New();
        var a = ctx.AddPc(100, 1000, "Alice"); a.GuildId = 7;
        var b = ctx.AddPc(200, 2000, "Bob"); b.GuildId = 7;
        var aSess = ctx.SessionOf(a); var bSess = ctx.SessionOf(b);
        ClearOutbound(aSess); ClearOutbound(bSess);

        await ctx.GuildHandler.HandleAsync(aSess, MakeGuild("Alice : guild rally!"));

        AssertSent(aSess, (ushort)PacketHeader.ZC_GUILD_CHAT);
        AssertSent(bSess, (ushort)PacketHeader.ZC_GUILD_CHAT);
        Assert.Equal(1, ctx.Ipc.GuildCalls);
    }

    // --- wire helpers ---

    private static ushort HeaderOf(byte[] packet)
        => (ushort)(packet[0] | (packet[1] << 8));

    private static IReadOnlyList<byte[]> OutboundQueue(MapSessionData session)
    {
        var queueField = typeof(Core.Server.Network.ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (queueField?.GetValue(session) is ConcurrentQueue<byte[]> q) return q.ToArray();
        return Array.Empty<byte[]>();
    }

    private static void ClearOutbound(MapSessionData session)
    {
        var queueField = typeof(Core.Server.Network.ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (queueField?.GetValue(session) is ConcurrentQueue<byte[]> q) q.Clear();
    }

    private static void AssertSent(MapSessionData session, ushort packetId)
    {
        var ids = OutboundQueue(session).Select(HeaderOf).ToArray();
        Assert.Contains(packetId, ids);
    }

    private static void AssertNotSent(MapSessionData session, ushort packetId)
    {
        var ids = OutboundQueue(session).Select(HeaderOf).ToArray();
        Assert.DoesNotContain(packetId, ids);
    }

    private static CZ_WHISPER MakeWhisper(string target, string text)
    {
        var p = new CZ_WHISPER();
        typeof(CZ_WHISPER).GetProperty("TargetName")!.SetValue(p, target);
        typeof(CZ_WHISPER).GetProperty("Text")!.SetValue(p, text);
        return p;
    }

    private static CZ_REQUEST_CHAT_PARTY MakeParty(string text)
    {
        var p = new CZ_REQUEST_CHAT_PARTY();
        typeof(CZ_REQUEST_CHAT_PARTY).GetProperty("Text")!.SetValue(p, text);
        return p;
    }

    private static CZ_GUILD_CHAT MakeGuild(string text)
    {
        var p = new CZ_GUILD_CHAT();
        typeof(CZ_GUILD_CHAT).GetProperty("Text")!.SetValue(p, text);
        return p;
    }

    // --- harness ---

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var sessions = new InMemorySessions();
        var ipc = new StubChatIpc();
        var svc = new ChatService(entities, sessions, ipc, NullLogger<ChatService>.Instance);
        return new TestContext(
            svc, entities, sessions, ipc,
            new WhisperHandler(entities, svc, NullLogger<WhisperHandler>.Instance),
            new PartyChatHandler(entities, svc, NullLogger<PartyChatHandler>.Instance),
            new GuildChatHandler(entities, svc, NullLogger<GuildChatHandler>.Instance),
            (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        ChatService Service,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        StubChatIpc Ipc,
        WhisperHandler WhisperHandler,
        PartyChatHandler PartyHandler,
        GuildChatHandler GuildHandler,
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

    private sealed class StubChatIpc : IChatIpcOutbound
    {
        public int WhisperCalls;
        public int PartyCalls;
        public int GuildCalls;
        public string LastWhisperTarget = string.Empty;
        public int LastPartyId;

        public Task<bool> WhisperAsync(int senderAccountId, long senderCharacterId,
            string senderName, string targetName, string message, CancellationToken ct = default)
        {
            WhisperCalls++;
            LastWhisperTarget = targetName;
            return Task.FromResult(true);
        }

        public Task<bool> PartyMessageAsync(int partyId, int senderAccountId, string message, CancellationToken ct = default)
        {
            PartyCalls++;
            LastPartyId = partyId;
            return Task.FromResult(true);
        }

        public Task<bool> GuildMessageAsync(int guildId, int senderAccountId, string message, CancellationToken ct = default)
        {
            GuildCalls++;
            return Task.FromResult(true);
        }
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
