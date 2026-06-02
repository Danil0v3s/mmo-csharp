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
using Map.Server.Tests.Skills.Parity;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-49 — Basilica sanctuary. rAthena battle_calc_damage (RENEWAL): a target
/// with SC_BASILICA_CELL takes no damage from any attack, unless the attacker has
/// MD_STATUSIMMUNE (boss/MVP) or the skill is SP_SOULEXPLOSION. Because the hit is
/// fully blocked, a mid-cast caster on a Basilica cell is never interrupted.
/// </summary>
public class Combat49BasilicaTests
{
    [Fact]
    public void Basilica_cell_target_takes_no_melee_damage()
    {
        var ctx = New();
        var attacker = NewSwinger(range: 1, swing: 80);
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);
        ctx.Sc.Start(victim, StatusType.BasilicaCell, 0, 0, 0, 0, 60_000);

        Assert.Equal(0, ctx.Service.PerformMeleeAttack(attacker, victim).Total);
        Assert.Equal(1000, victim.Hp);
    }

    [Fact]
    public void Normal_target_takes_melee_damage()
    {
        var ctx = New();
        var attacker = NewSwinger(range: 1, swing: 80);
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);

        ctx.Service.PerformMeleeAttack(attacker, victim);
        Assert.True(victim.Hp < 1000); // control: no Basilica → damage lands
    }

    [Fact]
    public void Status_immune_attacker_bypasses_basilica()
    {
        var ctx = New();
        var attacker = NewSwinger(range: 1, swing: 80);
        attacker.Stats.Mode |= MobMode.StatusImmune; // boss / MVP attacker
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);
        ctx.Sc.Start(victim, StatusType.BasilicaCell, 0, 0, 0, 0, 60_000);

        ctx.Service.PerformMeleeAttack(attacker, victim);
        Assert.True(victim.Hp < 1000); // MD_STATUSIMMUNE ignores the sanctuary
    }

    [Fact]
    public void IsBasilicaImmune_true_only_for_basilica_target_and_normal_attacker()
    {
        var ctx = New();
        var attacker = NewSwinger(range: 1, swing: 80);
        var victim = ctx.AddMob(52, 50, hp: 1000);

        // No SC → not immune.
        Assert.False(ctx.Service.IsBasilicaImmune(victim, attacker));

        ctx.Sc.Start(victim, StatusType.BasilicaCell, 0, 0, 0, 0, 60_000);
        Assert.True(ctx.Service.IsBasilicaImmune(victim, attacker));

        // Status-immune attacker bypasses.
        attacker.Stats.Mode |= MobMode.StatusImmune;
        Assert.False(ctx.Service.IsBasilicaImmune(victim, attacker));
    }

    [Fact]
    public void Casting_basilica_target_is_not_interrupted()
    {
        var cast = new RecordingCast();
        var ctx = New(cast);
        var attacker = NewSwinger(range: 1, swing: 80);
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);
        ctx.Sc.Start(victim, StatusType.BasilicaCell, 0, 0, 0, 0, 60_000);

        ctx.Service.PerformMeleeAttack(attacker, victim);
        Assert.Equal(0, cast.CancelCalls); // 0 damage → cast survives
    }

    [Fact]
    public void Casting_normal_target_is_interrupted()
    {
        var cast = new RecordingCast();
        var ctx = New(cast);
        var attacker = NewSwinger(range: 1, swing: 80);
        ctx.Place(attacker, 50, 50);
        var victim = ctx.AddMob(52, 50, hp: 1000);

        ctx.Service.PerformMeleeAttack(attacker, victim);
        Assert.Equal(1, cast.CancelCalls); // control: real damage interrupts
    }

    // ---- helpers ----

    private static PlayerEntity NewSwinger(int range, int swing)
    {
        var p = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        p.Stats.Dex = (short)swing; p.Stats.WeaponLevel = 0;
        p.Stats.WatkMin = p.Stats.WatkMax = (ushort)swing;
        p.Stats.Batk = 0; p.Stats.Cri = 0; p.Stats.Hit = 10000;
        p.Stats.AttackRange = (short)range;
        return p;
    }

    private static Ctx New(RecordingCast? cast = null)
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
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var service = new DamageService(
            visibility, mobSpawn, entities, new BattleCalculator(new Random(0)),
            NullLogger<DamageService>.Instance,
            services: cast != null ? new StubServices(cast) : null,
            sc: new Lazy<IStatusChangeService>(() => sc));
        return new Ctx(service, entities, ids, sc, (uint)mapName.GetHashCode());
    }

    private sealed record Ctx(DamageService Service, EntityRegistry Entities, EntityIdAllocator Ids,
        RecordingStatusChangeService Sc, uint MapId)
    {
        public void Place(PlayerEntity p, short x, short y) { p.MapId = MapId; p.X = x; p.Y = y; Entities.Add(p); }
        public MobEntity AddMob(short x, short y, int hp)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = hp };
            Entities.Add(m);
            return m;
        }
    }

    private sealed class StubServices : IServiceProvider
    {
        private readonly ISkillCastService _cast;
        public StubServices(ISkillCastService cast) => _cast = cast;
        public object? GetService(Type serviceType)
            => serviceType == typeof(ISkillCastService) ? _cast : null;
    }

    private sealed class RecordingCast : ISkillCastService
    {
        public int CancelCalls;
        public bool IsCasting(EntityId entityId) => true;
        public (ushort skillId, ushort skillLevel) GetCurrentCast(EntityId entityId) => (SkillIds.WZ_STORMGUST, 1);
        public bool CancelCast(EntityId entityId) { CancelCalls++; return true; }
        public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel) => SkillCastResult.Started;
        public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel) => false;
        public void Tick(long nowTick) { }
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
