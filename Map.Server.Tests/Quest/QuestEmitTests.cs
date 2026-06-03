using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Quest;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Quest;

/// <summary>
/// GP-QUEST — quest client emits: ZC_UPDATE_MISSION_HUNT on objective progress, ZC_DEL_QUEST on delete.
/// </summary>
public class QuestEmitTests
{
    [Fact]
    public void MobKill_emits_update_mission_hunt_with_live_count()
    {
        var (svc, pc, session) = Build(new QuestDbEntity { QuestId = 1000, Mob1 = "PORING", Count1 = 3 });
        svc.Add(pc, 1000);

        svc.UpdateMobObjective(pc, "PORING"); // count 0 → 1

        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_UPDATE_MISSION_HUNT);
        Assert.Equal(b.Length, BitConverter.ToUInt16(b, 2));      // len field == actual
        Assert.Equal(1, BitConverter.ToInt16(b, 4));              // one objective
        Assert.Equal(1000, BitConverter.ToInt32(b, 6));          // quest id
        Assert.Equal(1_000_000, BitConverter.ToInt32(b, 10));    // quest index = id*1000 + 0
        Assert.Equal(3, BitConverter.ToInt16(b, 14));            // target
        Assert.Equal(1, BitConverter.ToInt16(b, 16));            // current
    }

    [Fact]
    public void Delete_emits_del_quest()
    {
        var (svc, pc, session) = Build(new QuestDbEntity { QuestId = 1000, Mob1 = "PORING", Count1 = 3 });
        svc.Add(pc, 1000);

        svc.Delete(pc, 1000);

        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_DEL_QUEST);
        Assert.Equal(1000, BitConverter.ToInt32(b, 2));
    }

    // --- helpers ---

    private static (QuestService svc, PlayerEntity pc, MapSessionData session) Build(params QuestDbEntity[] catalog)
    {
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), 1, 50, 50) { Hp = 1000, MaxHp = 1000 };
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, EntityId = pc.Id };
        var sessions = new FakeSessions(pc.Id, session);
        var svc = new QuestService(NullLogger<QuestService>.Instance, sessions);
        svc.SeedCatalogForTest(catalog);
        return (svc, pc, session);
    }

    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
    }
}
