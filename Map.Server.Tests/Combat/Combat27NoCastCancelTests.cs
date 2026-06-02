using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-27 — SC-based / GvG-gated no-cast-cancel states in the damage
/// interrupt gate. rAthena unit_skillcastcancel (unit.cpp): exempt when
/// no_castcancel2 (unconditional) OR ((SC_UNLIMITEDHUMMINGVOICE || no_castcancel)
/// AND not GvG/BG).
/// </summary>
public class Combat27NoCastCancelTests
{
    [Fact]
    public void NoCastCancel2_is_exempt_unconditionally_even_on_gvg()
    {
        var ctx = Build(gvg: true);
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        caster.EquipBonuses.NoCastCancel2 = true;
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(caster, 100);
        Assert.True(ctx.Cast.IsCasting(caster.Id));      // survives even in GvG
        Assert.Equal(0, ctx.Cast.CancelCount);
    }

    [Fact]
    public void NoCastCancel_is_exempt_on_a_normal_map()
    {
        var ctx = Build(gvg: false);
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        caster.EquipBonuses.NoCastCancel = true;
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(caster, 100);
        Assert.True(ctx.Cast.IsCasting(caster.Id));      // exempt on a normal map
        Assert.Equal(0, ctx.Cast.CancelCount);
    }

    [Fact]
    public void NoCastCancel_is_interrupted_on_a_gvg_map()
    {
        var ctx = Build(gvg: true);
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        caster.EquipBonuses.NoCastCancel = true;         // GvG-gated → no protection here
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(caster, 100);
        Assert.False(ctx.Cast.IsCasting(caster.Id));     // interrupted in GvG
        Assert.Equal(1, ctx.Cast.CancelCount);
    }

    [Fact]
    public void UnlimitedHummingVoice_sc_is_exempt_on_a_normal_map()
    {
        var ctx = Build(gvg: false);
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        ctx.Sc.Start(caster, StatusType.Unlimitedhummingvoice, val1: 1, 0, 0, 0, durationMs: 10_000);
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(caster, 100);
        Assert.True(ctx.Cast.IsCasting(caster.Id));      // SC exempts on a normal map
        Assert.Equal(0, ctx.Cast.CancelCount);
    }

    // ---------- rig ----------

    private static TestContext Build(bool gvg)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyItemCatalog(), itemDrops,
            movement, visibility, ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));

        var cast = new FakeCastService();
        var skillClient = new SkillClientService(visibility, NullLogger<SkillClientService>.Instance);
        var skillDb = new SkillDb();
        var provider = new TestServiceProvider(new Dictionary<Type, object>
        {
            [typeof(ISkillCastService)] = cast,
            [typeof(ISkillClientService)] = skillClient,
            [typeof(ISkillDb)] = skillDb,
        });
        var flags = new StubFlags(gvg ? mapName : null);

        // Two-phase: placeholder damage → SC → real damage holding the SC.
        var placeholder = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(placeholder, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance,
            mapFlags: flags, maps: world, services: provider,
            sc: new Lazy<IStatusChangeService>(() => sc));

        return new TestContext(damage, cast, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        DamageService Damage, FakeCastService Cast, StatusChangeService Sc,
        EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int hp)
        {
            var p = new PlayerEntity(1, 10, "Wizard", Guid.NewGuid(), MapId, x, y);
            p.Stats.MaxHp = hp; p.Hp = hp;
            Entities.Add(p);
            return p;
        }
    }

    private sealed class FakeCastService : ISkillCastService
    {
        private readonly Dictionary<int, ushort> _casting = new();
        public int CancelCount { get; private set; }
        public void Begin(EntityId id, ushort skillId) => _casting[id.Value] = skillId;
        public bool IsCasting(EntityId entityId) => _casting.ContainsKey(entityId.Value);
        public (ushort skillId, ushort skillLevel) GetCurrentCast(EntityId entityId)
            => _casting.TryGetValue(entityId.Value, out var s) ? (s, (ushort)1) : ((ushort)0, (ushort)0);
        public bool CancelCast(EntityId entityId)
        {
            if (!_casting.Remove(entityId.Value)) return false;
            CancelCount++;
            return true;
        }
        public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel) => false;
        public void Tick(long nowTick) { }
        public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
        {
            Begin(source.Id, skillId);
            return SkillCastResult.Started;
        }
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _map;
        public TestServiceProvider(Dictionary<Type, object> map) => _map = map;
        public object? GetService(Type serviceType) => _map.GetValueOrDefault(serviceType);
    }

    private sealed class StubFlags : IMapFlagService
    {
        private readonly string? _gvgMap;
        public StubFlags(string? gvgMap) => _gvgMap = gvgMap;
        public bool IsSet(string mapName, MapFlag flag)
            => flag == MapFlag.Gvg && _gvgMap != null && string.Equals(mapName, _gvgMap, StringComparison.OrdinalIgnoreCase);
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

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string aegisName) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }
}
