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

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-85 — the skill_db Unit.Flag overlay (curated from db/re/skill_db.yml), the rAthena
/// e_skill_unit_flag bit re-alignment, and the live consumer: the UF_NOREITERATION place-gate.
/// </summary>
public class Combat85UnitFlagsTests
{
    // ---- enum re-alignment to rAthena e_skill_unit_flag ----

    [Fact]
    public void SkillUnitFlag_bits_match_the_rathena_enum_order()
    {
        Assert.Equal(1u << 0, (uint)SkillUnitFlag.NoEnemy);
        Assert.Equal(1u << 1, (uint)SkillUnitFlag.NoReiteration);
        Assert.Equal(1u << 3, (uint)SkillUnitFlag.NoOverlap);
        Assert.Equal(1u << 15, (uint)SkillUnitFlag.RemovedByFireRain); // UF_REMOVEDBYFIRERAIN
        Assert.Equal(1u << 16, (uint)SkillUnitFlag.KnockbackGroup);    // UF_KNOCKBACKGROUP (was missing)
        Assert.Equal(1u << 17, (uint)SkillUnitFlag.HiddenTrap);        // UF_HIDDENTRAP (was one bit low)
    }

    // ---- the curated overlay populates UnitFlags for ≥2 known skills ----

    [Fact]
    public void Overlay_loads_the_unit_flags_for_handled_skills()
    {
        var db = new SkillDb();
        db.Register(Def(SkillIds.MG_SAFETYWALL, "MG_SAFETYWALL"));
        db.Register(Def(SkillIds.PR_SANCTUARY, "PR_SANCTUARY"));
        db.Register(Def(SkillIds.WZ_STORMGUST, "WZ_STORMGUST"), revalidate: true);

        Assert.True(db.GetUnitFlag(SkillIds.MG_SAFETYWALL, SkillUnitFlag.NoReiteration));
        Assert.False(db.GetUnitFlag(SkillIds.MG_SAFETYWALL, SkillUnitFlag.NoOverlap));
        Assert.True(db.GetUnitFlag(SkillIds.PR_SANCTUARY, SkillUnitFlag.NoOverlap));
        Assert.True(db.GetUnitFlag(SkillIds.PR_SANCTUARY, SkillUnitFlag.PathCheck));
        Assert.False(db.GetUnitFlag(SkillIds.WZ_STORMGUST, SkillUnitFlag.NoReiteration));
    }

    // ---- the live consumer: UF_NOREITERATION place-gate ----

    [Fact]
    public void NoReiteration_blocks_a_second_overlapping_unit_of_the_same_skill()
    {
        var db = new SkillDb();
        db.Register(Def(SkillIds.MG_SAFETYWALL, "MG_SAFETYWALL"), revalidate: true); // gets NoReiteration
        var ctx = Build(db);
        var caster = ctx.AddPlayer(1, 100, 100);

        Assert.NotNull(ctx.Units.Place(caster, SkillIds.MG_SAFETYWALL, 5, 110, 110));  // first lands
        Assert.Null(ctx.Units.Place(caster, SkillIds.MG_SAFETYWALL, 5, 110, 110));     // same cell → blocked
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.MG_SAFETYWALL, 5, 112, 110));  // far cell → OK
    }

    [Fact]
    public void A_reiterable_skill_without_the_flag_can_overlap()
    {
        // No db (no flags loaded) → NoReiteration not set → placements stack freely.
        var ctx = Build(db: null);
        var caster = ctx.AddPlayer(1, 100, 100);
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.MG_SAFETYWALL, 5, 110, 110));
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.MG_SAFETYWALL, 5, 110, 110));
    }

    // ---- harness ----

    private static SkillDefinition Def(ushort id, string name) => new()
    {
        Id = id, Name = name, MaxLevel = 10,
        Target = SkillTargetMode.Ground, DamageKind = SkillDamageKind.Magic,
    };

    private static TestContext Build(ISkillDb? db)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var visibility = new VisibilityService(entities, new RecordingDispatcher());
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyCatalog(), itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var registry = new SkillUnitTickRegistry(new ISkillUnitTickHandler[] { new SafetyWallUnit() });
        var units = new SkillUnitService(entities, registry, new SkillUnitContext(damage),
            NullLogger<SkillUnitService>.Instance, layouts: null, db: db);
        return new TestContext(units, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(SkillUnitService Units, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
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
