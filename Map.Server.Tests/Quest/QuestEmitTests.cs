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
    public void Add_emits_add_quest_and_mission()
    {
        var (svc, pc, session) = Build(new QuestDbEntity { QuestId = 1000, Mob1 = "PORING", Count1 = 3 });

        svc.Add(pc, 1000);

        var add = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_ADD_QUEST);
        Assert.Equal(143, add.Length);                          // fixed-size primary
        Assert.Equal(1000, BitConverter.ToInt32(add, 2));       // quest id
        Assert.Equal(1, add[6]);                                // state = active
        Assert.Equal(1, BitConverter.ToInt16(add, 15));         // numObjectives
        // obj0 at offset 17: questIndex.L effect.L mob_id.L(25) minLv.W maxLv.W current.W name.24(35)
        Assert.Equal(1_000_000, BitConverter.ToInt32(add, 17)); // quest index
        Assert.Equal(1002, BitConverter.ToInt32(add, 25));      // resolved mob id (PORING)
        Assert.Equal("Poring", ReadCString(add, 35, 24));       // resolved mob name

        var mission = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_ADD_QUEST_MISSION);
        Assert.Equal(mission.Length, BitConverter.ToUInt16(mission, 2)); // len field
        Assert.Equal(1_000_000, BitConverter.ToInt32(mission, 4));       // quest index
        Assert.Equal(1002, BitConverter.ToInt32(mission, 8));           // mob id
        Assert.Equal(3, BitConverter.ToInt16(mission, 12));            // target
        Assert.Equal(0, BitConverter.ToInt16(mission, 14));           // current
    }

    [Fact]
    public void PcLogin_emits_all_quest_list_snapshot_with_live_counts()
    {
        var (svc, pc, session) = Build(new QuestDbEntity { QuestId = 1000, Mob1 = "PORING", Count1 = 3 });
        svc.Add(pc, 1000);
        svc.UpdateMobObjective(pc, "PORING"); // count 0 → 1, so the snapshot must carry current=1

        var active = svc.PcLogin(pc);
        Assert.Equal(1, active);

        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_ALL_QUEST_LIST);
        Assert.Equal(b.Length, BitConverter.ToUInt16(b, 2));   // len field == actual
        Assert.Equal(1, BitConverter.ToInt32(b, 4));           // quest count
        // quest row at offset 8: quest_id.L state.B timeDiff.L time.L numObj.W (15B)
        Assert.Equal(1000, BitConverter.ToInt32(b, 8));        // quest id
        Assert.Equal(1, b[12]);                                // state = active
        Assert.Equal(1, BitConverter.ToInt16(b, 21));          // numObjectives
        // objective at offset 23: questIndex.L effect.L mob_id.L minLv.W maxLv.W current.W target.W name.24
        Assert.Equal(1_000_000, BitConverter.ToInt32(b, 23));  // quest index = id*1000 + 0
        Assert.Equal(1002, BitConverter.ToInt32(b, 31));       // resolved mob id (PORING)
        Assert.Equal(1, BitConverter.ToInt16(b, 39));          // current (live count after one kill)
        Assert.Equal(3, BitConverter.ToInt16(b, 41));          // target
        Assert.Equal("Poring", ReadCString(b, 43, 24));        // resolved mob name
    }

    private static string ReadCString(byte[] b, int off, int width)
    {
        var end = off; while (end < off + width && b[end] != 0) end++;
        return System.Text.Encoding.ASCII.GetString(b, off, end - off);
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
        var svc = new QuestService(NullLogger<QuestService>.Instance, sessions, new FakeMobDb());
        svc.SeedCatalogForTest(catalog);
        return (svc, pc, session);
    }

    private sealed class FakeMobDb : Map.Server.Mob.IMobDb
    {
        private readonly Map.Server.Mob.MobDbEntry _poring = new() { Id = 1002, AegisName = "PORING", Name = "Poring" };
        public int Count => 1;
        public Map.Server.Mob.MobDbEntry? Get(int classId) => classId == 1002 ? _poring : null;
        public Map.Server.Mob.MobDbEntry? GetByAegisName(string aegisName)
            => string.Equals(aegisName, "PORING", StringComparison.OrdinalIgnoreCase) ? _poring : null;
        public IEnumerable<Map.Server.Mob.MobDbEntry> All() => new[] { _poring };
        public void Reload() { }
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
