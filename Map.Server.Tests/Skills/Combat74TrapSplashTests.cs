using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Skills.Splash;
using Map.Server.Skills.Units;
using Map.Server.Skills.Units.Handlers;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;
using Map.Server.World;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-74 — Ranger trap detonation: a Range-3 splash hits every enemy in range (not just the
/// stepper), applies the trap's on-hit SC, and consumes the trap unit.
/// </summary>
public class Combat74TrapSplashTests
{
    private const string MapName = "test";
    private static readonly uint MapId = (uint)MapName.GetHashCode();

    [Fact]
    public void FiringTrap_detonation_splashes_applies_burning_and_consumes()
    {
        var entities = NewWorld();
        var caster = NewRanger(40, 40);
        entities.Add(caster);
        var onTrap = NewMob(2, 50, 50);   // stepper, on the trap cell
        var nearby = NewMob(3, 52, 50);   // within Range 3
        var faraway = NewMob(4, 60, 50);  // outside Range 3
        foreach (var m in new[] { onTrap, nearby, faraway }) entities.Add(m);

        var splash = new MapForeachInRangeService(entities);
        var units = new RecordingUnits();
        var dmg = new RecordingDamage();
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var ctx = new Ctx(dmg, sc);

        var trap = new FiringTrapUnit(splash, new Lazy<ISkillUnitService>(() => units));
        var group = NewTrapGroup(SkillIds.RA_FIRINGTRAP, caster.Id, MapId, x: 50, y: 50);

        trap.OnPlace(caster, onTrap, skillLevel: 5, tick: 0, ctx, group);

        // Both in-range mobs were damaged + burned; the far one was not.
        Assert.True(dmg.Hits.ContainsKey(onTrap.Id));
        Assert.True(dmg.Hits.ContainsKey(nearby.Id));
        Assert.False(dmg.Hits.ContainsKey(faraway.Id));
        Assert.NotNull(sc.Get(onTrap, StatusType.Burning));
        Assert.NotNull(sc.Get(nearby, StatusType.Burning));
        Assert.Null(sc.Get(faraway, StatusType.Burning));

        // The trap was consumed on detonation.
        Assert.Contains(group, units.Deleted);
    }

    [Fact]
    public void IceboundTrap_applies_freezing_on_splash()
    {
        var entities = NewWorld();
        var caster = NewRanger(40, 40);
        var victim = NewMob(2, 50, 50);
        entities.Add(caster); entities.Add(victim);

        var splash = new MapForeachInRangeService(entities);
        var units = new RecordingUnits();
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var trap = new IceboundTrapUnit(splash, new Lazy<ISkillUnitService>(() => units));
        var group = NewTrapGroup(SkillIds.RA_ICEBOUNDTRAP, caster.Id, MapId, 50, 50);

        trap.OnPlace(caster, victim, skillLevel: 5, tick: 0, new Ctx(new RecordingDamage(), sc), group);

        Assert.NotNull(sc.Get(victim, StatusType.Freezing));
        Assert.Contains(group, units.Deleted);
    }

    [Fact]
    public void A_second_stepper_does_not_re_detonate_a_consumed_trap()
    {
        var entities = NewWorld();
        var caster = NewRanger(40, 40);
        var first = NewMob(2, 50, 50);
        entities.Add(caster); entities.Add(first);

        var splash = new MapForeachInRangeService(entities);
        var units = new RecordingUnits();
        var dmg = new RecordingDamage();
        var trap = new ClusterBombUnit(splash, new Lazy<ISkillUnitService>(() => units));
        var group = NewTrapGroup(SkillIds.RA_CLUSTERBOMB, caster.Id, MapId, 50, 50);

        trap.OnPlace(caster, first, 5, 0, new Ctx(dmg, null), group); // detonates + consumes
        var hitsAfterFirst = dmg.TotalHits;
        trap.OnPlace(caster, first, 5, 0, new Ctx(dmg, null), group); // group already removed → no-op
        Assert.Equal(hitsAfterFirst, dmg.TotalHits);
    }

    // ---- helpers ----

    private static EntityRegistry NewWorld()
    {
        var world = new StubWorld(new MapData(MapName, 100, 100, new byte[100 * 100]));
        return new EntityRegistry(world);
    }

    private static PlayerEntity NewRanger(short x, short y)
    {
        var pc = new PlayerEntity(1, 1, "Ranger", Guid.NewGuid(), MapId, x, y);
        pc.Stats.Dex = 100; pc.Stats.IntStat = 50; pc.Level = 150;
        pc.LearnedSkills[SkillIds.RA_RESEARCHTRAP] = 5;
        return pc;
    }

    private static MobEntity NewMob(int id, short x, short y)
        => new(new EntityId(id), 1002, "Poring", mapId: MapId, x: x, y: y) { Hp = 100000, MaxHp = 100000 };

    private static SkillUnitGroup NewTrapGroup(ushort skillId, EntityId caster, uint mapId, short x, short y)
    {
        var g = new SkillUnitGroup
        {
            SkillId = skillId, SkillLevel = 5, CasterId = caster, MapId = mapId,
            ExpiresAt = long.MaxValue, IntervalMs = 1000,
        };
        g.Units.Add(new SkillUnit { Group = g, X = x, Y = y, NextTick = 0 });
        return g;
    }

    private sealed class Ctx : ISkillUnitContext
    {
        public Ctx(IDamageService d, IStatusChangeService? sc) { Damage = d; Sc = sc; }
        public IDamageService Damage { get; }
        public IStatusChangeService? Sc { get; }
        public ISkillClientService? Client => null;
    }

    private sealed class RecordingDamage : IDamageService
    {
        public readonly Dictionary<EntityId, int> Hits = new();
        public int TotalHits;
        public int ApplyDamage(Entity target, int damage, Entity? source = null, int hits = 1)
        {
            Hits[target.Id] = damage; TotalHits++; return damage;
        }
        public BattleDamage PerformMeleeAttack(Entity source, Entity target) => default;
    }

    private sealed class RecordingUnits : ISkillUnitService
    {
        public readonly List<SkillUnitGroup> Deleted = new();
        public void DelUnitGroup(SkillUnitGroup group)
        {
            foreach (var u in group.Units) u.Removed = true;
            Deleted.Add(group);
        }
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
        public void DelUnit(SkillUnit unit) { }
        public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius) => Array.Empty<SkillUnit>();
        public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius, ushort skillId) => Array.Empty<SkillUnit>();
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
}
