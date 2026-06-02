using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Entities;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Units;
using Map.Server.Skills.Units.Handlers;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-47 — Land Protector place-gate + the skill-funnel ground-unit intercept.
/// <para>rAthena <c>skill_unitsetting</c> refuses to place a ground unit on an
/// SA_LANDPROTECTOR cell unless the skill carries <c>UF_NOLP</c>; and
/// <c>battle_calc_damage</c> lets a Safety Wall (melee) / Pneuma (ranged) cell
/// intercept a weapon <b>skill</b>, not just the auto-attack.</para>
/// </summary>
public class Combat47LandProtectorTests
{
    // ---- Land Protector place-gate (SkillUnitService.Place) --------------

    [Fact]
    public void GroundUnit_refused_on_a_LandProtector_cell()
    {
        var ctx = Build(db: null);
        var caster = ctx.AddPlayer(1, 100, 100);

        // Lay down Land Protector at (110, 110).
        var lp = ctx.Units.Place(caster, SkillIds.SA_LANDPROTECTOR, 5, 110, 110);
        Assert.NotNull(lp);

        // Storm Gust refused on the protected cell …
        Assert.Null(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 110, 110));
        // … but places freely on a clear cell.
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 130, 130));
    }

    [Fact]
    public void IgnoreLandProtector_skill_places_on_a_LandProtector_cell()
    {
        // COMBAT-66 — the exemption is INF2_IGNORELANDPROTECTOR (not a unit flag); a skill_db
        // carrying it for Storm Gust exempts it from the gate.
        var db = new SkillDb();
        db.Register(new SkillDefinition
        {
            Id = SkillIds.WZ_STORMGUST,
            Name = "WZ_STORMGUST",
            MaxLevel = 10,
            Target = SkillTargetMode.Ground,
            DamageKind = SkillDamageKind.Magic,
            Inf2 = SkillInf2.IgnoreLandProtector,
        });

        var ctx = Build(db);
        var caster = ctx.AddPlayer(1, 100, 100);
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.SA_LANDPROTECTOR, 5, 110, 110));

        // INF2_IGNORELANDPROTECTOR → placement succeeds despite the Land Protector underneath.
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 110, 110));
    }

    // ---- skill-funnel ground-unit intercept (DamageService.TryGroundUnitBlock) ----

    [Fact]
    public void SafetyWall_blocks_a_melee_skill_and_consumes_the_pool()
    {
        var units = new StubUnits();
        var svc = NewDamageService(units);
        var victim = new MobEntity(new EntityId(50), 1002, "Poring", mapId: 7, x: 52, y: 50) { Hp = 1000 };
        var wall = units.AddGroup(SkillIds.MG_SAFETYWALL, victim.MapId, 52, 50, val2: 2);

        // The weapon-skill funnel calls TryGroundUnitBlock(target, isShortRange).
        Assert.True(svc.TryGroundUnitBlock(victim, isShortRange: true));
        Assert.Equal(1, wall.Val2);
        Assert.True(svc.TryGroundUnitBlock(victim, isShortRange: true));
        Assert.Equal(0, wall.Val2);
        Assert.Contains(wall, units.Removed);
    }

    [Fact]
    public void SafetyWall_does_not_block_a_ranged_skill()
    {
        var units = new StubUnits();
        var svc = NewDamageService(units);
        var victim = new MobEntity(new EntityId(51), 1002, "Poring", mapId: 7, x: 52, y: 50) { Hp = 1000 };
        units.AddGroup(SkillIds.MG_SAFETYWALL, victim.MapId, 52, 50, val2: 2);

        Assert.False(svc.TryGroundUnitBlock(victim, isShortRange: false));
    }

    [Fact]
    public void Pneuma_blocks_a_ranged_skill_but_not_a_melee_skill()
    {
        var units = new StubUnits();
        var svc = NewDamageService(units);
        var victim = new MobEntity(new EntityId(52), 1002, "Poring", mapId: 7, x: 52, y: 50) { Hp = 1000 };
        units.AddGroup(SkillIds.AL_PNEUMA, victim.MapId, 52, 50, val2: 0);

        Assert.True(svc.TryGroundUnitBlock(victim, isShortRange: false));
        Assert.False(svc.TryGroundUnitBlock(victim, isShortRange: true));
    }

    // ================= harness =================

    private DamageService NewDamageService(StubUnits units)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyCatalog(), itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        return new DamageService(visibility, mobSpawn, entities, new BattleCalculator(new Random(0)),
            NullLogger<DamageService>.Instance, services: new StubServices(units));
    }

    private static TestContext Build(ISkillDb? db)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyCatalog(), itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var registry = new SkillUnitTickRegistry(new ISkillUnitTickHandler[]
        {
            new StubLandProtectorUnit(),
            new StormGustUnit(),
        });
        var unitCtx = new SkillUnitContext(damage);
        var units = new SkillUnitService(entities, registry, unitCtx,
            NullLogger<SkillUnitService>.Instance, layouts: null, db: db);
        return new TestContext(units, entities, ids, (uint)mapName.GetHashCode());
    }

    /// <summary>Minimal single-cell Land Protector unit so Place can create the group
    /// (the production LP unit handler is COMBAT-66).</summary>
    private sealed class StubLandProtectorUnit : ISkillUnitTickHandler
    {
        public ushort SkillId => SkillIds.SA_LANDPROTECTOR;
        public int DurationMs(ushort skillLevel) => 60000;
        public int IntervalMs(ushort skillLevel) => 1000;
        public int Radius(ushort skillLevel) => 0;
        public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }
    }

    private sealed record TestContext(
        SkillUnitService Units, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }
    }

    private sealed class StubUnits : ISkillUnitService
    {
        private readonly List<SkillUnitGroup> _groups = new();
        public readonly List<SkillUnitGroup> Removed = new();

        public SkillUnitGroup AddGroup(ushort skillId, uint mapId, short x, short y, int val2)
        {
            var g = new SkillUnitGroup
            {
                SkillId = skillId, SkillLevel = 1, CasterId = new EntityId(1000),
                MapId = mapId, ExpiresAt = long.MaxValue, IntervalMs = 1000, Val2 = val2,
            };
            g.Units.Add(new SkillUnit { Group = g, X = x, Y = y, NextTick = 0 });
            _groups.Add(g);
            return g;
        }

        public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius)
            => _groups.Where(g => g.MapId == mapId)
                      .SelectMany(g => g.Units)
                      .Where(u => !u.Removed && Math.Abs(u.X - cx) <= radius && Math.Abs(u.Y - cy) <= radius)
                      .ToList();

        public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius, ushort skillId)
            => GetUnitsInArea(mapId, cx, cy, radius).Where(u => u.Group.SkillId == skillId).ToList();

        public void DelUnitGroup(SkillUnitGroup group)
        {
            foreach (var u in group.Units) u.Removed = true;
            _groups.Remove(group);
            Removed.Add(group);
        }

        public void DelUnit(SkillUnit unit) => unit.Removed = true;
        public SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short cx, short cy) => null;
        public SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short cx, short cy, int delayMs) => null;
        public void Tick(long nowTick) { }
        public void UnitMove(Entity who, long tick, int flag) { }
        public void UnitMoveUnit(SkillUnit unit, short newX, short newY) { }
        public void UnitMoveUnitGroup(SkillUnitGroup group, short newX, short newY) { }
        public void UnitOnLeft(SkillUnit unit, Entity who, long tick) { }
        public void UnitOnOut(SkillUnit unit, Entity who, long tick) { }
        public void UnitOnDamaged(SkillUnit unit, long damage) { }
        public void ClearUnitGroup(EntityId casterId) { }
    }

    private sealed class StubServices : IServiceProvider
    {
        private readonly ISkillUnitService _units;
        public StubServices(ISkillUnitService units) => _units = units;
        public object? GetService(Type serviceType)
            => serviceType == typeof(ISkillUnitService) ? _units : null;
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

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string aegisName) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class EmptyCatalog : IItemCatalog
    {
        public int Count => 0;
        public ItemEntity? Get(uint itemId) => null;
        public ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }
}
