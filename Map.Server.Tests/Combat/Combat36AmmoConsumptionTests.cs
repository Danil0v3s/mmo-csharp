using System;
using System.Collections.Generic;
using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-36 — ranged ammo consumption + no-ammo / wrong-ammo gate. rAthena
/// battle_weapon_attack refuses the swing when a bow/gun has no valid equipped
/// ammo (battle.cpp:10386) and battle_consume_ammo spends one round per swing.
/// </summary>
public class Combat36AmmoConsumptionTests
{
    private const uint Arrow = 1750;
    private const uint Bullet = 13200;

    [Fact]
    public void Melee_weapon_never_needs_ammo()
    {
        var (svc, pc, _) = Build(WeaponTypeCodes.Dagger, ammoSubtype: null, ammoAmount: 0);
        Assert.True(svc.HasUsableAmmo(pc));
        Assert.True(svc.ConsumeAmmo(pc)); // no-op, succeeds
    }

    [Fact]
    public void Bow_with_arrows_consumes_one_per_swing_then_refuses_at_zero()
    {
        var (svc, pc, session) = Build(WeaponTypeCodes.Bow, "Arrow", ammoAmount: 3);

        Assert.True(svc.HasUsableAmmo(pc));
        Assert.True(svc.ConsumeAmmo(pc));
        Assert.Equal(2u, AmmoStack(session));
        Assert.True(svc.ConsumeAmmo(pc));
        Assert.Equal(1u, AmmoStack(session));
        Assert.True(svc.ConsumeAmmo(pc));      // last round → stack removed

        // At 0 arrows the stack is gone and the next swing is refused.
        Assert.Null(FindAmmo(session));
        Assert.False(svc.HasUsableAmmo(pc));
        Assert.False(svc.ConsumeAmmo(pc));
    }

    [Fact]
    public void Bow_with_no_ammo_is_gated()
    {
        var (svc, pc, _) = Build(WeaponTypeCodes.Bow, "Arrow", ammoAmount: 0);
        Assert.False(svc.HasUsableAmmo(pc));
    }

    [Fact]
    public void Arrow_in_a_gun_does_not_fire_it()
    {
        // A Revolver requires Bullet ammo; an equipped Arrow is treated as no ammo.
        var (svc, pc, _) = Build(WeaponTypeCodes.Revolver, "Arrow", ammoAmount: 10);
        Assert.False(svc.HasUsableAmmo(pc));
    }

    [Fact]
    public void Gun_with_matching_bullet_fires()
    {
        var (svc, pc, session) = Build(WeaponTypeCodes.Revolver, "Bullet", ammoAmount: 10);
        Assert.True(svc.HasUsableAmmo(pc));
        Assert.True(svc.ConsumeAmmo(pc));
        Assert.Equal(9u, AmmoStack(session));
    }

    // ---- harness ----

    private static uint AmmoStack(MapSessionData s) => FindAmmo(s)?.Amount ?? 0;

    private static InventoryItem? FindAmmo(MapSessionData s)
    {
        foreach (var i in s.Inventory!)
            if ((i.Equip & EquipBonusAggregator.EquipAmmo) != 0) return i;
        return null;
    }

    private static (AmmoService svc, PlayerEntity pc, MapSessionData session) Build(
        int weaponType, string? ammoSubtype, uint ammoAmount)
    {
        var pc = new PlayerEntity(1, 1, "Archer", Guid.NewGuid(), 0, 0, 0) { WeaponType = weaponType };

        var catalog = new StubCatalog();
        var inv = new List<InventoryItem>();
        if (ammoSubtype != null)
        {
            var ammoId = ammoSubtype == "Bullet" ? Bullet : Arrow;
            catalog.Set(ammoId, new ItemEntity { Type = "Ammo", Subtype = ammoSubtype, Attack = 25 });
            inv.Add(new InventoryItem
            {
                Id = 99, ServerIndex = 0, NameId = ammoId, Amount = ammoAmount,
                Equip = EquipBonusAggregator.EquipAmmo,
            });
        }

        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(
            sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry,
            NullLogger.Instance)
        {
            AccountId = 1, CharacterId = 1, EntityId = pc.Id, AuthState = MapAuthState.Spawned,
            Inventory = inv,
        };

        var sessions = new InMemorySessions();
        sessions.Register(pc.Id, session);
        var svc = new AmmoService(sessions, catalog, NullLogger<AmmoService>.Instance);
        return (svc, pc, session);
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
