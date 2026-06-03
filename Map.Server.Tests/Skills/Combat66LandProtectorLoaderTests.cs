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
/// COMBAT-66 — the live data + handler that make the COMBAT-47 Land Protector gate fire
/// in production: the curated <c>INF2_IGNORELANDPROTECTOR</c> overlay (the rAthena-correct
/// exemption — not a unit flag) and the placeable <see cref="LandProtectorUnit"/>.
/// </summary>
public class Combat66LandProtectorLoaderTests
{
    // ---- INF2_IGNORELANDPROTECTOR from the skill_db inf2 column (COMBAT-92) ----

    [Fact]
    public void Inf2_column_marks_ignore_land_protector_skills()
    {
        var db = new SkillDb();
        db.Register(Def(SkillIds.AC_SHOWER, "AC_SHOWER", "IgnoreLandProtector"));
        db.Register(Def(SkillIds.SG_SUN_WARM, "SG_SUN_WARM", "IgnoreLandProtector"));
        db.Register(Def(SkillIds.WZ_STORMGUST, "WZ_STORMGUST"), revalidate: true); // control

        Assert.True(db.GetInf2(SkillIds.AC_SHOWER, SkillInf2.IgnoreLandProtector));
        Assert.True(db.GetInf2(SkillIds.SG_SUN_WARM, SkillInf2.IgnoreLandProtector));
        Assert.False(db.GetInf2(SkillIds.WZ_STORMGUST, SkillInf2.IgnoreLandProtector));
    }

    // ---- production LandProtectorUnit handler values ----

    [Fact]
    public void LandProtector_handler_matches_rathena_duration_and_radius()
    {
        var h = new LandProtectorUnit();
        Assert.Equal(SkillIds.SA_LANDPROTECTOR, h.SkillId);
        Assert.Equal(165_000, h.DurationMs(1)); // 120000 + 45000*1
        Assert.Equal(345_000, h.DurationMs(5)); // 120000 + 45000*5
        Assert.Equal(1, h.Radius(1)); // 3x3
        Assert.Equal(2, h.Radius(3)); // 5x5
        Assert.Equal(3, h.Radius(5)); // 7x7
    }

    // ---- end-to-end gate with the real handler ----

    [Fact]
    public void Real_handler_covers_the_area_and_gates_hostile_ground_skills()
    {
        var ctx = Build(db: null);
        var caster = ctx.AddPlayer(1, 100, 100);

        // Land Protector lv5 → radius 3 → covers (107..113) in both axes.
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.SA_LANDPROTECTOR, 5, 110, 110));

        Assert.Null(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 110, 110)); // center
        Assert.Null(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 113, 110)); // edge of the 7x7
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 114, 110)); // just outside
    }

    [Fact]
    public void IgnoreLandProtector_skill_places_through_the_real_handler()
    {
        var db = new SkillDb();
        db.Register(new SkillDefinition
        {
            Id = SkillIds.WZ_STORMGUST, Name = "WZ_STORMGUST", MaxLevel = 10,
            Target = SkillTargetMode.Ground, DamageKind = SkillDamageKind.Magic,
            Inf2 = SkillInf2.IgnoreLandProtector,
        }, revalidate: true);

        var ctx = Build(db);
        var caster = ctx.AddPlayer(1, 100, 100);
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.SA_LANDPROTECTOR, 5, 110, 110));
        Assert.NotNull(ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 110, 110));
    }

    // ---- harness (mirrors COMBAT-47, but with the production LandProtectorUnit) ----

    // COMBAT-92 — Inf2 now comes from the skill_db `inf2` column via FromEntity (seed populates it
    // from db/re/skill_db.yml Flags), not the retired curated overlay.
    private static SkillDefinition Def(ushort id, string name, string inf2 = "") =>
        SkillDbLoader.FromEntity(new SkillDbEntity
        {
            Id = id, Name = name, MaxLevel = 10,
            TargetMode = "Ground", DamageKind = "Magic", Inf2 = inf2,
        });

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
            new LandProtectorUnit(), // the production handler
            new StormGustUnit(),
        });
        var unitCtx = new SkillUnitContext(damage);
        var units = new SkillUnitService(entities, registry, unitCtx,
            NullLogger<SkillUnitService>.Instance, layouts: null, db: db);
        return new TestContext(units, entities, ids, (uint)mapName.GetHashCode());
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
