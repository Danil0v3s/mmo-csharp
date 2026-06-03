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
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Tests.Skills.Parity;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-76 — per-skill skill_db ammo columns (mask + qty) drive the gate/consume, the
/// NW_MAGAZINE_FOR_ONE + W_GATLING +4 special, and the exact fail packet (arrow_fail /
/// NEED_MORE_BULLET / NEED_EQUIPMENT_KUNAI). rAthena skill_get_ammotype / skill_get_ammo_qty
/// + skill_check_condition_castbegin + battle_consume_ammo.
/// </summary>
public class Combat76SkillAmmoDataTests
{
    private const int AmmoArrowBit = 1 << 1, AmmoBulletBit = 1 << 3, AmmoKunaiBit = 1 << 7;

    // ---- the curated overlay populates GetAmmoType / GetAmmoQty ----

    [Fact]
    public void Overlay_loads_per_skill_mask_and_qty()
    {
        var db = Db(SkillIds.RA_ARROWSTORM, maxLevel: 10);
        Assert.Equal(AmmoArrowBit, db.GetAmmoType(SkillIds.RA_ARROWSTORM));
        Assert.Equal(5, db.GetAmmoQty(SkillIds.RA_ARROWSTORM, 5));   // AmmoAmount: 5, all levels

        var gun = Db(SkillIds.GS_DESPERADO, maxLevel: 10);
        Assert.Equal(AmmoBulletBit, gun.GetAmmoType(SkillIds.GS_DESPERADO));
        Assert.Equal(10, gun.GetAmmoQty(SkillIds.GS_DESPERADO, 1)); // AmmoAmount: 10
    }

    [Fact]
    public void A_skill_without_an_ammo_requirement_reports_zero()
    {
        var db = Db(SkillIds.SM_BASH, maxLevel: 10);
        Assert.Equal(0, db.GetAmmoType(SkillIds.SM_BASH));
    }

    // ---- multi-round consume ----

    [Fact]
    public void Arrow_storm_consumes_its_exact_five_round_qty()
    {
        var ctx = Cast(SkillIds.RA_ARROWSTORM, WeaponTypeCodes.Bow, "Arrow", ammoAmount: 8, lvl: 5);
        ctx.svc.ResolveSkill(ctx.pc, ctx.target, SkillIds.RA_ARROWSTORM, 5);
        Assert.Equal(3u, AmmoStack(ctx.session)); // 8 - 5 = 3
    }

    [Fact]
    public void Arrow_storm_with_four_arrows_is_gated_and_sends_arrow_fail()
    {
        var ctx = Cast(SkillIds.RA_ARROWSTORM, WeaponTypeCodes.Bow, "Arrow", ammoAmount: 4, lvl: 5);
        var result = ctx.svc.StartCast(ctx.pc, ctx.target.Id, SkillIds.RA_ARROWSTORM, 5);
        Assert.Equal(SkillCastResult.NeedAmmo, result);
        Assert.Equal("NoAmmo", (string)ctx.rec.Events.Single(e => e.Kind == "arrow-fail").Data["type"]!);
    }

    [Fact]
    public void Magazine_for_one_on_a_gatling_adds_four_rounds()
    {
        // base AmmoAmount 6 + W_GATLING special +4 = 10 consumed.
        var ctx = Cast(SkillIds.NW_MAGAZINE_FOR_ONE, WeaponTypeCodes.Gatling, "Bullet", ammoAmount: 12, lvl: 1);
        ctx.svc.ResolveSkill(ctx.pc, ctx.target, SkillIds.NW_MAGAZINE_FOR_ONE, 1);
        Assert.Equal(2u, AmmoStack(ctx.session)); // 12 - 10 = 2
    }

    [Fact]
    public void Magazine_for_one_gates_on_qty_plus_four()
    {
        var ctx = Cast(SkillIds.NW_MAGAZINE_FOR_ONE, WeaponTypeCodes.Gatling, "Bullet", ammoAmount: 9, lvl: 1);
        var result = ctx.svc.StartCast(ctx.pc, ctx.target.Id, SkillIds.NW_MAGAZINE_FOR_ONE, 1);
        Assert.Equal(SkillCastResult.NeedAmmo, result); // 9 < 10
        Assert.Equal("NeedMoreBullet", (string)ctx.rec.Events.Single(e => e.Kind == "fail").Data["cause"]!);
    }

    // ---- fail packet selection by ammo type ----

    [Fact]
    public void Gun_skill_out_of_bullets_sends_need_more_bullet()
    {
        var ctx = Cast(SkillIds.GS_DESPERADO, WeaponTypeCodes.Revolver, "Bullet", ammoAmount: 0, lvl: 1);
        var result = ctx.svc.StartCast(ctx.pc, ctx.target.Id, SkillIds.GS_DESPERADO, 1);
        Assert.Equal(SkillCastResult.NeedAmmo, result);
        Assert.Equal("NeedMoreBullet", (string)ctx.rec.Events.Single(e => e.Kind == "fail").Data["cause"]!);
    }

    [Fact]
    public void Kunai_skill_is_weapon_independent_and_sends_need_equipment_kunai()
    {
        // A dagger weapon does NOT use ammo, yet KO_HAPPOKUNAI requires Kunai (explicit mask).
        var ctx = Cast(SkillIds.KO_HAPPOKUNAI, WeaponTypeCodes.Dagger, "Kunai", ammoAmount: 1, lvl: 1);
        var result = ctx.svc.StartCast(ctx.pc, ctx.target.Id, SkillIds.KO_HAPPOKUNAI, 1);
        Assert.Equal(SkillCastResult.NeedAmmo, result); // needs 2, has 1
        Assert.Equal("NeedEquipmentKunai", (string)ctx.rec.Events.Single(e => e.Kind == "fail").Data["cause"]!);
    }

    [Fact]
    public void Kunai_skill_consumes_kunai_with_a_non_ammo_weapon()
    {
        var ctx = Cast(SkillIds.KO_HAPPOKUNAI, WeaponTypeCodes.Dagger, "Kunai", ammoAmount: 5, lvl: 1);
        ctx.svc.ResolveSkill(ctx.pc, ctx.target, SkillIds.KO_HAPPOKUNAI, 1);
        Assert.Equal(3u, AmmoStack(ctx.session)); // 5 - 2 = 3
    }

    // ---- helpers ----

    private const uint AmmoNameId = 1750;

    private static SkillDb Db(ushort skillId, byte maxLevel)
    {
        var db = new SkillDb();
        db.Register(new SkillDefinition
        {
            Id = skillId, Name = skillId.ToString(), MaxLevel = maxLevel,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Weapon, Range = 9,
        }, revalidate: true);
        return db;
    }

    private sealed record CastBundle(SkillCastService svc, PlayerEntity pc, MobEntity target,
        MapSessionData session, SkillTraceRecorder rec);

    private static CastBundle Cast(ushort skillId, int weaponType, string ammoSubtype, uint ammoAmount, ushort lvl)
    {
        var pc = new PlayerEntity(1, 1, "Shooter", Guid.NewGuid(), 0, 50, 50) { WeaponType = weaponType };
        pc.LearnedSkills[skillId] = 10;
        pc.Sp = 1000;

        var catalog = new StubCatalog();
        catalog.Set(AmmoNameId, new ItemEntity { Type = "Ammo", Subtype = ammoSubtype, Attack = 25 });
        var inv = new List<InventoryItem>
        {
            new() { Id = 99, ServerIndex = 0, NameId = AmmoNameId, Amount = ammoAmount, Equip = EquipBonusAggregator.EquipAmmo },
        };

        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 1, CharacterId = 1, EntityId = pc.Id, AuthState = MapAuthState.Spawned, Inventory = inv,
        };
        var sessions = new InMemorySessions();
        sessions.Register(pc.Id, session);
        var ammo = new AmmoService(sessions, catalog, NullLogger<AmmoService>.Instance);

        var world = new StubWorld(new MapData("m", 100, 100, new byte[100 * 100]));
        var entities = new EntityRegistry(world);
        entities.Add(pc);
        var target = new MobEntity(new EntityId(2002), 1002, "Poring", 0, 51, 50) { Hp = 5000, MaxHp = 5000 };
        entities.Add(target);

        var rec = new SkillTraceRecorder();
        var client = new RecordingSkillClientService(rec);
        var damage = new DamageService(new VisibilityService(entities, new RecordingDispatcher()),
            new NoOpMobSink(), entities, new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var svc = new SkillCastService(
            Db(skillId, 10), entities,
            new SkillResolverRegistry(Array.Empty<ISkillResolver>()),
            NullLogger<SkillCastService>.Instance,
            ammo: ammo, client: client);
        return new CastBundle(svc, pc, target, session, rec);
    }

    private static uint AmmoStack(MapSessionData s)
        => s.Inventory!.FirstOrDefault(i => i.NameId == AmmoNameId)?.Amount ?? 0;

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
