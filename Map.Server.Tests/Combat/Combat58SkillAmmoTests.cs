using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Skills;
using Map.Server.Skills.Resolvers;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Tests.Skills.Parity;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.Spawn;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-58 — ammo gate + consume on ammo-using SKILLS (rAthena
/// skill_check_condition_castbegin ammo arm + battle_consume_ammo). Extends the
/// COMBAT-36 auto-attack ammo wiring to the skill cast path.
/// </summary>
public class Combat58SkillAmmoTests
{
    private const uint Arrow = 1750;
    private const ushort BashLv = 1;

    // ---- AmmoService qty methods ----

    [Fact]
    public void HasUsableAmmo_checks_the_quantity()
    {
        var (ammo, pc, _) = Ammo(WeaponTypeCodes.Bow, amount: 3);
        Assert.True(ammo.HasUsableAmmo(pc, 3));
        Assert.False(ammo.HasUsableAmmo(pc, 4)); // not enough for 4 rounds
    }

    [Fact]
    public void ConsumeAmmo_spends_a_quantity_in_one_call()
    {
        var (ammo, pc, session) = Ammo(WeaponTypeCodes.Bow, amount: 5);
        Assert.True(ammo.ConsumeAmmo(pc, 2));
        Assert.Equal(3u, AmmoStack(session));
    }

    [Fact]
    public void ConsumeAmmo_qty_is_a_noop_for_melee()
    {
        var (ammo, pc, _) = Ammo(WeaponTypeCodes.Dagger, amount: 0);
        Assert.True(ammo.ConsumeAmmo(pc, 3)); // no-op, succeeds
    }

    // ---- skill cast path ----

    [Fact]
    public void Bow_skill_with_no_ammo_is_gated_and_sends_arrow_fail()
    {
        var rec = new SkillTraceRecorder();
        var ctx = SkillCtx(WeaponTypeCodes.Bow, ammoAmount: 0, rec);

        var result = ctx.svc.StartCast(ctx.pc, ctx.target.Id, SkillIds.SM_BASH, BashLv);

        Assert.Equal(SkillCastResult.NeedAmmo, result);
        // COMBAT-76 — arrows → clif_arrow_fail (ZC_ACTION_FAILURE), not clif_skill_fail.
        var fail = rec.Events.Single(e => e.Kind == "arrow-fail");
        Assert.Equal("NoAmmo", (string)fail.Data["type"]!);
    }

    [Fact]
    public void Bow_skill_resolve_consumes_a_round()
    {
        var rec = new SkillTraceRecorder();
        var ctx = SkillCtx(WeaponTypeCodes.Bow, ammoAmount: 3, rec);

        ctx.svc.ResolveSkill(ctx.pc, ctx.target, SkillIds.SM_BASH, BashLv);
        Assert.Equal(2u, AmmoStack(ctx.session)); // one round spent at castend
    }

    [Fact]
    public void Melee_skill_never_consumes_ammo()
    {
        var rec = new SkillTraceRecorder();
        var ctx = SkillCtx(WeaponTypeCodes.Dagger, ammoAmount: 0, rec);
        var result = ctx.svc.StartCast(ctx.pc, ctx.target.Id, SkillIds.SM_BASH, BashLv);
        Assert.NotEqual(SkillCastResult.NeedAmmo, result); // no ammo gate for melee
    }

    // ---- helpers ----

    private static (AmmoService ammo, PlayerEntity pc, MapSessionData session) Ammo(int weaponType, uint amount)
    {
        var pc = new PlayerEntity(1, 1, "Archer", Guid.NewGuid(), 0, 0, 0) { WeaponType = weaponType };
        var catalog = new StubCatalog();
        var inv = new List<InventoryItem>();
        if (weaponType != WeaponTypeCodes.Dagger)
        {
            catalog.Set(Arrow, new ItemEntity { Type = "Ammo", Subtype = "Arrow", Attack = 25 });
            inv.Add(new InventoryItem { Id = 99, ServerIndex = 0, NameId = Arrow, Amount = amount, Equip = EquipBonusAggregator.EquipAmmo });
        }
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 1, CharacterId = 1, EntityId = pc.Id, AuthState = MapAuthState.Spawned, Inventory = inv,
        };
        var sessions = new InMemorySessions();
        sessions.Register(pc.Id, session);
        return (new AmmoService(sessions, catalog, NullLogger<AmmoService>.Instance), pc, session);
    }

    private sealed record SkillCtxBundle(SkillCastService svc, PlayerEntity pc, MobEntity target, MapSessionData session);

    private static SkillCtxBundle SkillCtx(int weaponType, uint ammoAmount, SkillTraceRecorder rec)
    {
        var (ammo, pc, session) = Ammo(weaponType, ammoAmount);
        pc.LearnedSkills[SkillIds.SM_BASH] = 10;
        pc.Sp = 1000;
        pc.MapId = 0; pc.X = 50; pc.Y = 50;

        var world = new StubWorld(new MapData("m", 100, 100, new byte[100 * 100]));
        var entities = new EntityRegistry(world);
        entities.Add(pc);
        var target = new MobEntity(new EntityId(2002), 1002, "Poring", 0, 51, 50) { Hp = 5000, MaxHp = 5000 };
        entities.Add(target);

        var client = new RecordingSkillClientService(rec);
        var damage = new DamageService(new VisibilityService(entities, new RecordingDispatcher()),
            new NoOpMobSink(), entities, new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var svc = new SkillCastService(
            new SkillDb(), entities,
            new SkillResolverRegistry(Array.Empty<ISkillResolver>()),
            NullLogger<SkillCastService>.Instance,
            ammo: ammo, client: client);
        return new SkillCtxBundle(svc, pc, target, session);
    }

    private static uint AmmoStack(MapSessionData s)
        => s.Inventory!.FirstOrDefault(i => i.NameId == Arrow)?.Amount ?? 0;

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _bySid = new();
        public void Register(EntityId id, MapSessionData s) => _bySid[id] = s;
        public MapSessionData? GetByEntityId(EntityId entityId) => _bySid.GetValueOrDefault(entityId);
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

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly MapData _m;
        public StubWorld(MapData m) => _m = m;
        public MapData? Get(string name) => string.Equals(name, _m.Name, StringComparison.OrdinalIgnoreCase) ? _m : null;
        public IEnumerable<MapData> All => new[] { _m };
        public int TotalCells => _m.CellCount;
        public bool Contains(string name) => string.Equals(name, _m.Name, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NoOpMobSink : IMobDeathSink
    {
        public bool KillMob(EntityId id, PlayerEntity? lastHitter) => false;
    }
}
