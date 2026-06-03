using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.ItemEffects;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Tests.Skills.Parity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-94 — a partial consume (ammo round spent / potion used) immediately tells the owning
/// client the new stack count via rAthena <c>clif_delitem</c> (<c>ZC_DELETE_ITEM_FROM_BODY</c>,
/// 0x07fa), instead of leaving a stale count until the next periodic state sync. A full-slot
/// removal still rides the <c>RemovedInventoryIds</c> sync.
/// </summary>
public class Combat94InventoryDelItemTests
{
    private const uint Arrow = 1750;
    private const ushort DelItemHeader = (ushort)PacketHeader.ZC_DELETE_ITEM_FROM_BODY; // 0x07fa

    // ---- ammo consume (AmmoService) ----

    [Fact]
    public void Partial_ammo_consume_emits_delitem_with_consumed_count_and_client_index()
    {
        var (svc, pc, session) = BuildAmmo(WeaponTypeCodes.Bow, "Arrow", ammoAmount: 8, serverIndex: 0);

        Assert.True(svc.ConsumeAmmo(pc, 5, ammoMask: 0)); // weapon-gated bow path; 8 - 5 = 3 left

        var frame = SingleDelItem(session);
        Assert.Equal(1, frame.deleteType);                 // rAthena reason 1 = used for a skill
        Assert.Equal(2, frame.index);                      // server slot 0 + 2
        Assert.Equal(5, frame.amount);                     // rounds consumed
    }

    [Fact]
    public void Full_ammo_consume_emits_no_delitem_and_uses_removed_ids()
    {
        var (svc, pc, session) = BuildAmmo(WeaponTypeCodes.Bow, "Arrow", ammoAmount: 3, serverIndex: 0);

        Assert.True(svc.ConsumeAmmo(pc, 3, ammoMask: 0)); // 3 - 3 = 0 → slot removed

        Assert.Empty(DelItems(session));                   // no immediate amount-update
        Assert.Contains(99, session.RemovedInventoryIds);  // full removal still tracked for the sync
    }

    [Fact]
    public void Single_round_partial_consume_reports_count_one()
    {
        var (svc, pc, session) = BuildAmmo(WeaponTypeCodes.Revolver, "Bullet", ammoAmount: 10, serverIndex: 3);
        Assert.True(svc.ConsumeAmmo(pc)); // 1 round

        var frame = SingleDelItem(session);
        Assert.Equal(5, frame.index);     // server slot 3 + 2
        Assert.Equal(1, frame.amount);
    }

    // ---- item use (ItemUseService) — same shared helper, rAthena reason 0 (Normal) ----

    [Fact]
    public void Partial_item_use_emits_delitem_normal_reason()
    {
        var (svc, pc, session) = BuildItem(stackAmount: 5, serverIndex: 2);

        Assert.True(svc.UseItem(pc, 0)); // 5 - 1 = 4 left

        var frame = SingleDelItem(session);
        Assert.Equal(0, frame.deleteType); // Normal
        Assert.Equal(4, frame.index);      // server slot 2 + 2
        Assert.Equal(1, frame.amount);
    }

    [Fact]
    public void Item_use_to_zero_emits_no_delitem_and_uses_removed_ids()
    {
        var (svc, pc, session) = BuildItem(stackAmount: 1, serverIndex: 0);
        Assert.True(svc.UseItem(pc, 0)); // 1 - 1 = 0 → removed

        Assert.Empty(DelItems(session));
        Assert.Contains(77, session.RemovedInventoryIds);
    }

    // ---- the shared helper directly ----

    [Fact]
    public void NotifyItemConsumed_is_a_noop_for_zero_count()
    {
        var (_, _, session) = BuildAmmo(WeaponTypeCodes.Bow, "Arrow", ammoAmount: 8, serverIndex: 0);
        session.NotifyItemConsumed(0, 0, reason: 0);
        Assert.Empty(DelItems(session));
    }

    // ---- helpers ----

    private static IReadOnlyList<(int deleteType, int index, int amount)> DelItems(MapSessionData session)
    {
        var field = typeof(Core.Server.Network.ClientSession)
            .GetField("_outgoingPackets", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field?.GetValue(session) is not ConcurrentQueue<byte[]> q) return Array.Empty<(int, int, int)>();
        var frames = new List<(int, int, int)>();
        foreach (var bytes in q.ToArray())
        {
            if (bytes.Length < 8) continue;
            if (BitConverter.ToUInt16(bytes, 0) != DelItemHeader) continue;
            frames.Add((BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 4), BitConverter.ToUInt16(bytes, 6)));
        }
        return frames;
    }

    private static (int deleteType, int index, int amount) SingleDelItem(MapSessionData session)
        => Assert.Single(DelItems(session));

    private static (AmmoService svc, PlayerEntity pc, MapSessionData session) BuildAmmo(
        int weaponType, string ammoSubtype, uint ammoAmount, int serverIndex)
    {
        var pc = new PlayerEntity(1, 1, "Archer", Guid.NewGuid(), 0, 0, 0) { WeaponType = weaponType };
        var catalog = new StubCatalog();
        var ammoId = ammoSubtype == "Bullet" ? 13200u : Arrow;
        catalog.Set(ammoId, new ItemEntity { Type = "Ammo", Subtype = ammoSubtype, Attack = 25 });
        var inv = new List<InventoryItem>
        {
            new() { Id = 99, ServerIndex = serverIndex, NameId = ammoId, Amount = ammoAmount, Equip = EquipBonusAggregator.EquipAmmo },
        };
        var session = NewSession(pc, inv);
        var sessions = new InMemorySessions();
        sessions.Register(pc.Id, session);
        return (new AmmoService(sessions, catalog, NullLogger<AmmoService>.Instance), pc, session);
    }

    private static (ItemUseService svc, PlayerEntity pc, MapSessionData session) BuildItem(uint stackAmount, int serverIndex)
    {
        var pc = new PlayerEntity(1, 1, "User", Guid.NewGuid(), 0, 0, 0);
        const uint potionId = 501;
        var catalog = new StubCatalog();
        catalog.Set(potionId, new ItemEntity { Id = potionId, Type = "Usable", NameAegis = "Red_Potion" });
        var inv = new List<InventoryItem>
        {
            new() { Id = 77, ServerIndex = serverIndex, NameId = potionId, Amount = stackAmount },
        };
        var session = NewSession(pc, inv);
        var sessions = new InMemorySessions();
        sessions.Register(pc.Id, session);
        var effects = new ItemEffectRegistry(new RecordingStatusChangeService(new SkillTraceRecorder()));
        // A hook dispatcher that "handles" the use so the stack decrements (rAthena consumes one
        // stack on a successful use regardless of script visibility).
        var svc = new ItemUseService(catalog, effects, sessions, NullLogger<ItemUseService>.Instance, new AlwaysHandleHook());
        return (svc, pc, session);
    }

    private static MapSessionData NewSession(PlayerEntity pc, List<InventoryItem> inv)
    {
        var sockets = TestSocketFactory.CreateSocketPair();
        return new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 1, CharacterId = 1, EntityId = pc.Id, AuthState = MapAuthState.Spawned, Inventory = inv,
        };
    }

    private sealed class AlwaysHandleHook : IItemHookDispatcher
    {
        public bool TryInvokeOnUse(MapSessionData session, PlayerEntity player, InventoryItem item) => true;
        public bool TryInvokeOnEquip(InventoryItem item, EquipBonusBundle bundle, PlayerEntity player, IReadOnlyList<InventoryItem> equipped) => false;
        public void TryInvokeOnUnequip(InventoryItem item, EquipBonusBundle bundle, PlayerEntity player, IReadOnlyList<InventoryItem> equipped) { }
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
        private readonly Dictionary<EntityId, MapSessionData> _bySid = new();
        public void Register(EntityId id, MapSessionData s) => _bySid[id] = s;
        public MapSessionData? GetByEntityId(EntityId entityId) => _bySid.GetValueOrDefault(entityId);
    }
}
