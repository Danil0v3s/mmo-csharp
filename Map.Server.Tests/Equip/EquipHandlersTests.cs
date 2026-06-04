using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Equip;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Equip;

/// <summary>
/// End-to-end wire coverage for equip / unequip. Drives the packet
/// handlers, asserts on the outbound ack and the resulting
/// inventory wear bits + recalculated weapon ATK.
/// </summary>
public class EquipHandlersTests
{
    [Fact]
    public async Task EquipWeapon_SetsRightHandBitAndRecalcsAtk()
    {
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);

        // Sword: only valid in right-hand, ATK 100.
        ctx.Catalog.Set(1101, new ItemEntity
        {
            Id = 1101, NameAegis = "Sword", Type = "Weapon",
            LocationRightHand = 1, Attack = 100, View = 1,
        });
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = true },
        };
        ClearOutbound(s);

        // Client sends client_index = server_index + 2.
        await ctx.EquipHandler.HandleAsync(s, MakeEquip(clientIndex: 2, position: EquipBits.HandR));

        AssertSentWithResult(s, (ushort)PacketHeader.ZC_REQ_WEAR_EQUIP_ACK, expectedResult: 0);
        Assert.Equal(EquipBits.HandR, s.Inventory[0].Equip);
        Assert.Equal(100, pc.Stats.WatkMin);
    }

    [Fact]
    public async Task EquipWeapon_RefreshesWeaponElementStatuses()
    {
        // SC-IMMUNE — a wear-state change runs status_change_refresh (re-resolves the weapon-element
        // endow SC family for the new weapon), so the equip recalc invokes IStatusChangeService.Refresh.
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);
        ctx.Catalog.Set(1101, new ItemEntity { Id = 1101, NameAegis = "Sword", Type = "Weapon", LocationRightHand = 1, Attack = 100, View = 1 });
        s.Inventory = new List<InventoryItem> { new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = true } };

        Assert.Equal(0, ctx.StatusChange.RefreshCalls);
        await ctx.EquipHandler.HandleAsync(s, MakeEquip(clientIndex: 2, position: EquipBits.HandR));

        Assert.True(ctx.StatusChange.RefreshCalls > 0); // refresh ran on the equip recalc
    }

    [Fact]
    public async Task EquipWeapon_ReplacesExistingRightHand()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);

        ctx.Catalog.Set(1101, new ItemEntity { Id = 1101, NameAegis = "Sword", LocationRightHand = 1, Attack = 100 });
        ctx.Catalog.Set(1102, new ItemEntity { Id = 1102, NameAegis = "Better_Sword", LocationRightHand = 1, Attack = 200 });
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = true, Equip = EquipBits.HandR },
            new() { ServerIndex = 1, NameId = 1102, Amount = 1, Identified = true },
        };
        ClearOutbound(s);

        await ctx.EquipHandler.HandleAsync(s, MakeEquip(clientIndex: 3, position: EquipBits.HandR));

        AssertSentWithResult(s, (ushort)PacketHeader.ZC_REQ_WEAR_EQUIP_ACK, 0);
        Assert.Equal(0u, s.Inventory[0].Equip); // old weapon dropped
        Assert.Equal(EquipBits.HandR, s.Inventory[1].Equip); // new weapon worn
        Assert.Equal(200, pc.Stats.WatkMin); // recalc picked up the better sword
    }

    [Fact]
    public async Task EquipUnidentified_AcksFailure()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);

        ctx.Catalog.Set(1101, new ItemEntity { Id = 1101, LocationRightHand = 1, Attack = 100 });
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = false },
        };
        ClearOutbound(s);

        await ctx.EquipHandler.HandleAsync(s, MakeEquip(clientIndex: 2, position: EquipBits.HandR));

        AssertSentWithResult(s, (ushort)PacketHeader.ZC_REQ_WEAR_EQUIP_ACK, expectedResult: 2);
        Assert.Equal(0u, s.Inventory[0].Equip);
    }

    [Fact]
    public async Task UnequipWornItem_ClearsBitsAndAcksSuccess()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);

        ctx.Catalog.Set(1101, new ItemEntity { Id = 1101, LocationRightHand = 1, Attack = 100 });
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = true, Equip = EquipBits.HandR },
        };
        ctx.Service.Equip(s, 0, EquipBits.HandR, out _); // make sure recalc baseline includes the weapon
        Assert.Equal(100, pc.Stats.WatkMin);
        ClearOutbound(s);

        await ctx.UnequipHandler.HandleAsync(s, MakeUnequip(clientIndex: 2));

        var ack = OutboundQueue(s).Single(b => HeaderOf(b) == (ushort)PacketHeader.ZC_REQ_TAKEOFF_EQUIP_ACK);
        Assert.Equal((byte)1, ack[8]); // flag at offset 2(hdr) + 2(idx) + 4(loc) = 8
        Assert.Equal(0u, s.Inventory[0].Equip);
        Assert.Equal(0, pc.Stats.WatkMin); // recalc dropped the weapon contribution
    }

    [Fact]
    public async Task UnequipBareItem_AcksFailure()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);

        ctx.Catalog.Set(1101, new ItemEntity { Id = 1101, LocationRightHand = 1, Attack = 100 });
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = true /* not worn */ },
        };
        ClearOutbound(s);

        await ctx.UnequipHandler.HandleAsync(s, MakeUnequip(clientIndex: 2));

        var ack = OutboundQueue(s).Single(b => HeaderOf(b) == (ushort)PacketHeader.ZC_REQ_TAKEOFF_EQUIP_ACK);
        Assert.Equal((byte)0, ack[8]); // flag = 0 → fail
    }

    [Fact]
    public async Task EquipAccessory_PicksRightSlotWhenLeftIsTaken()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);

        // Ring fits either accessory slot.
        ctx.Catalog.Set(2601, new ItemEntity { Id = 2601, LocationRightAccessory = 1, LocationLeftAccessory = 1 });
        ctx.Catalog.Set(2602, new ItemEntity { Id = 2602, LocationRightAccessory = 1, LocationLeftAccessory = 1 });
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 2601, Amount = 1, Identified = true, Equip = EquipBits.AccL }, // L already worn
            new() { ServerIndex = 1, NameId = 2602, Amount = 1, Identified = true },
        };
        ClearOutbound(s);

        // Client requests both bits (R | L); server should pick R since L is taken.
        await ctx.EquipHandler.HandleAsync(s, MakeEquip(clientIndex: 3, position: EquipBits.AccRL));

        AssertSentWithResult(s, (ushort)PacketHeader.ZC_REQ_WEAR_EQUIP_ACK, expectedResult: 0);
        Assert.Equal(EquipBits.AccL, s.Inventory[0].Equip); // existing L untouched
        Assert.Equal(EquipBits.AccR, s.Inventory[1].Equip); // new picked R
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

    private static void AssertSentWithResult(MapSessionData session, ushort packetId, byte expectedResult)
    {
        var packet = OutboundQueue(session).Single(b => HeaderOf(b) == packetId);
        // ZC_REQ_WEAR_EQUIP_ACK result byte sits at the end: 2(hdr) + 2(idx) + 4(loc) + 2(viewid) = 10.
        Assert.Equal(expectedResult, packet[10]);
    }

    private static CZ_REQ_WEAR_EQUIP MakeEquip(ushort clientIndex, uint position)
    {
        var p = new CZ_REQ_WEAR_EQUIP();
        typeof(CZ_REQ_WEAR_EQUIP).GetProperty("ClientIndex")!.SetValue(p, clientIndex);
        typeof(CZ_REQ_WEAR_EQUIP).GetProperty("Position")!.SetValue(p, position);
        return p;
    }

    private static CZ_REQ_TAKEOFF_EQUIP MakeUnequip(ushort clientIndex)
    {
        var p = new CZ_REQ_TAKEOFF_EQUIP();
        typeof(CZ_REQ_TAKEOFF_EQUIP).GetProperty("ClientIndex")!.SetValue(p, clientIndex);
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
        var catalog = new StubCatalog();
        var statusCalc = new RecalcOnlyStatusCalc();
        var statusChange = new Map.Server.Tests.Skills.Parity.RecordingStatusChangeService(
            new Map.Server.Tests.Skills.Parity.SkillTraceRecorder());
        var svc = new EquipService(catalog, entities, statusCalc, NullLogger<EquipService>.Instance,
            statusChange: statusChange);
        return new TestContext(
            svc, catalog, entities, sessions,
            new EquipItemHandler(entities, svc, catalog, NullLogger<EquipItemHandler>.Instance),
            new UnequipItemHandler(entities, svc, NullLogger<UnequipItemHandler>.Instance),
            (uint)mapName.GetHashCode(), statusChange);
    }

    private sealed record TestContext(
        EquipService Service,
        StubCatalog Catalog,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        EquipItemHandler EquipHandler,
        UnequipItemHandler UnequipHandler,
        uint MapId,
        Map.Server.Tests.Skills.Parity.RecordingStatusChangeService StatusChange)
    {
        public PlayerEntity AddPc(int charId, int accountId)
        {
            var pc = new PlayerEntity(charId, accountId, $"P{charId}", Guid.NewGuid(), MapId, 50, 50);
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

    /// <summary>
    /// Status calc stub that just folds the WeaponAtkMin/Max from the
    /// inputs into the player's stats so tests can assert on the recalc
    /// path firing.
    /// </summary>
    private sealed class RecalcOnlyStatusCalc : IStatusCalcService
    {
        public void CalcPc(PlayerEntity player, PcBaseInputs inputs)
        {
            player.Stats.WatkMin = (ushort)inputs.WeaponAtkMin;
            player.Stats.WatkMax = (ushort)inputs.WeaponAtkMax;
            player.Stats.Def = (short)inputs.EquipDef;
            player.Stats.AttackRange = (short)inputs.AttackRange;
        }
        public void CalcMob(Map.Server.Entities.MobEntity mob) { }
        public void CalcHomunculus(Map.Server.Entities.MobEntity homun, int levelOverride = 0) { }
        public void CalcMercenary(Map.Server.Entities.MobEntity merc, int levelOverride = 0) { }
        public void CalcElemental(Map.Server.Entities.MobEntity ele, int levelOverride = 0) { }
        public void CalcNpc(Map.Server.Entities.NpcEntity npc) { }
    }

    private sealed class StubCatalog : IItemCatalog
    {
        private readonly Dictionary<uint, ItemEntity> _byId = new();
        public void Set(uint id, ItemEntity row) => _byId[id] = row;
        public int Count => _byId.Count;
        public ItemEntity? Get(uint id) => _byId.GetValueOrDefault(id);
        public ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<ItemEntity> All() => _byId.Values;
        public void Reload() { }
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
