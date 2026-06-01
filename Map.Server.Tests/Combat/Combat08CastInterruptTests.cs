using System;
using System.Collections.Generic;
using System.Linq;
using Core.Server.Packets.Out.ZC;
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
/// COMBAT-08 — damage-driven cast interrupt. rAthena status_fix_damage →
/// unit_skillcastcancel(target,2) on a surviving hit and
/// unit_skillcastcancel(target,0) on death. Asserts the cast is aborted, the
/// real <see cref="ZC_SKILL_CAST_CANCEL"/> packet is emitted to the caster's
/// AOI, and the <c>bNoCastCancel</c> gate exempts a flagged caster.
/// </summary>
public class Combat08CastInterruptTests
{
    [Fact]
    public void SurvivingHit_CancelsCancellableCast_AndEmitsPacket()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH); // SM_BASH → CastCancel=true (fallback default)

        ctx.Damage.ApplyDamage(caster, 100); // non-lethal

        Assert.False(ctx.Cast.IsCasting(caster.Id));      // cast aborted
        Assert.Equal(1, ctx.Cast.CancelCount);
        var cancel = ctx.Dispatcher.Sent
            .Select(s => s.packet).OfType<ZC_SKILL_CAST_CANCEL>().SingleOrDefault();
        Assert.NotNull(cancel);                            // real packet, not a log line
        Assert.Equal(caster.Id.Value, cancel!.Gid);
    }

    [Fact]
    public void NoCastCancel_Caster_IsNotInterrupted()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        caster.EquipBonuses.NoCastCancel = true;          // bNoCastCancel(2)
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(caster, 100);

        Assert.True(ctx.Cast.IsCasting(caster.Id));        // cast survives
        Assert.Equal(0, ctx.Cast.CancelCount);
        Assert.Empty(ctx.Dispatcher.Sent.Select(s => s.packet).OfType<ZC_SKILL_CAST_CANCEL>());
    }

    [Fact]
    public void ZeroDamage_DoesNotInterrupt()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(100, 100, hp: 1000);
        ctx.Cast.Begin(caster.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(caster, 0); // miss → no interrupt (rAthena gates on actual>0)

        Assert.True(ctx.Cast.IsCasting(caster.Id));
        Assert.Equal(0, ctx.Cast.CancelCount);
    }

    [Fact]
    public void Death_CancelsCast_Unconditionally()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100, hp: 50);
        ctx.Cast.Begin(mob.Id, SkillIds.SM_BASH);

        ctx.Damage.ApplyDamage(mob, 9999); // lethal

        Assert.False(ctx.Cast.IsCasting(mob.Id));
        Assert.Equal(1, ctx.Cast.CancelCount);
    }

    // ---------- Test rig ----------

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, mobDb, new EmptyItemCatalog(), itemDrops,
            movement, visibility, ids, new StatusCalcService(),
            NullLogger<MobSpawnService>.Instance, new Random(0));

        var cast = new FakeCastService();
        var skillClient = new SkillClientService(visibility, NullLogger<SkillClientService>.Instance);
        var skillDb = new SkillDb(); // fallback — SM_BASH present, CastCancel defaults true
        var provider = new TestServiceProvider(new Dictionary<Type, object>
        {
            [typeof(ISkillCastService)] = cast,
            [typeof(ISkillClientService)] = skillClient,
            [typeof(ISkillDb)] = skillDb,
        });

        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance,
            services: provider);

        return new TestContext(damage, cast, entities, dispatcher, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        DamageService Damage, FakeCastService Cast, EntityRegistry Entities,
        RecordingDispatcher Dispatcher, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int hp)
        {
            var p = new PlayerEntity(1, 10, "Wizard", Guid.NewGuid(), MapId, x, y);
            p.Stats.MaxHp = hp; p.Hp = hp;
            Entities.Add(p);
            return p;
        }

        public MobEntity AddMob(short x, short y, int hp)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = hp };
            m.Stats.MaxHp = hp;
            Entities.Add(m);
            return m;
        }
    }

    /// <summary>Minimal <see cref="ISkillCastService"/> double: tracks a single
    /// in-progress cast per entity and records cancel calls.</summary>
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
