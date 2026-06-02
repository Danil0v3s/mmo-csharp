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
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-25 — defensive ground-unit damage intercept. Safety Wall blocks melee
/// (consuming its block pool); Pneuma blocks ranged. rAthena battle_calc_damage
/// MG_SAFETYWALL / AL_PNEUMA cell checks.
/// </summary>
public class Combat25GroundUnitBlockTests
{
    [Fact]
    public void SafetyWall_blocks_melee_and_consumes_the_pool()
    {
        var units = new StubUnits();
        var ctx = NewContext(units);
        var attacker = NewSwinger(range: 1, swing: 80); // melee
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);
        var wall = units.AddGroup(SkillIds.MG_SAFETYWALL, victim.MapId, 52, 50, val2: 2);

        // First two melee swings are blocked; the pool drains to 0 → wall removed.
        Assert.Equal(0, ctx.Service.PerformMeleeAttack(attacker, victim).Total);
        Assert.Equal(1000, victim.Hp);
        Assert.Equal(1, wall.Val2);

        ctx.Service.PerformMeleeAttack(attacker, victim);
        Assert.Equal(1000, victim.Hp);
        Assert.Equal(0, wall.Val2);
        Assert.Contains(wall, units.Removed);
    }

    [Fact]
    public void SafetyWall_does_not_block_ranged()
    {
        var units = new StubUnits();
        var ctx = NewContext(units);
        var attacker = NewSwinger(range: 5, swing: 80); // ranged
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);
        units.AddGroup(SkillIds.MG_SAFETYWALL, victim.MapId, 52, 50, val2: 2);

        ctx.Service.PerformMeleeAttack(attacker, victim);
        Assert.True(victim.Hp < 1000); // ranged passes through Safety Wall
    }

    [Fact]
    public void Pneuma_blocks_ranged_but_not_melee()
    {
        var units = new StubUnits();
        var ctx = NewContext(units);
        var victim1 = ctx.AddMob(52, 50, hp: 1000);
        units.AddGroup(SkillIds.AL_PNEUMA, victim1.MapId, 52, 50, val2: 0);

        var ranged = NewSwinger(range: 5, swing: 80);
        ctx.Place(ranged, 50, 50);
        Assert.Equal(0, ctx.Service.PerformMeleeAttack(ranged, victim1).Total);
        Assert.Equal(1000, victim1.Hp);

        // Melee passes through Pneuma.
        var melee = NewSwinger(range: 1, swing: 80, charId: 9);
        ctx.Place2(melee, 51, 50, charId: 9);
        ctx.Service.PerformMeleeAttack(melee, victim1);
        Assert.True(victim1.Hp < 1000);
    }

    // ---- helpers ----

    private static PlayerEntity NewSwinger(int range, int swing, int charId = 1)
    {
        var p = new PlayerEntity(charId, charId, "Hero", Guid.NewGuid(), 0, 0, 0);
        p.Stats.Dex = (short)swing; p.Stats.WeaponLevel = 0;
        p.Stats.WatkMin = p.Stats.WatkMax = (ushort)swing;
        p.Stats.Batk = 0; p.Stats.Cri = 0; p.Stats.Hit = 10000;
        p.Stats.AttackRange = (short)range;
        return p;
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

        // Unused by the intercept — no-op test surface.
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

    private Ctx NewContext(StubUnits units)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility, new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyCatalog(), itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var service = new DamageService(visibility, mobSpawn, entities, new BattleCalculator(new Random(0)),
            NullLogger<DamageService>.Instance, services: new StubServices(units));
        return new Ctx(service, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record Ctx(DamageService Service, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public void Place(PlayerEntity p, short x, short y) { p.MapId = MapId; p.X = x; p.Y = y; Entities.Add(p); }
        public void Place2(PlayerEntity p, short x, short y, int charId) { Place(p, x, y); }
        public MobEntity AddMob(short x, short y, int hp)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = hp };
            Entities.Add(m);
            return m;
        }
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
