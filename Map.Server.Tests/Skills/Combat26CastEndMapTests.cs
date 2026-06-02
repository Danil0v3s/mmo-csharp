using System;
using System.Collections.Generic;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Resolvers;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-26 — CastEndMap warp skills. rAthena skill_castend_map AL_TELEPORT:
/// "Random" → pc_randomwarp; "SavePoint" → pc_setpos(save_point); noteleport
/// maps refuse.
/// </summary>
public class Combat26CastEndMapTests
{
    [Fact]
    public void Teleport_random_warps_the_caster()
    {
        var pos = new StubPositions();
        var svc = NewService(pos, new StubDeath(), noTeleport: false);
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());

        Assert.True(svc.CastEndMap(pc, "Random", SkillIds.AL_TELEPORT));
        Assert.Equal(1, pos.RandomWarpCalls);
    }

    [Fact]
    public void Teleport_savepoint_warps_to_save()
    {
        var death = new StubDeath();
        var svc = NewService(new StubPositions(), death, noTeleport: false);
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());

        Assert.True(svc.CastEndMap(pc, "SavePoint", SkillIds.AL_TELEPORT));
        Assert.Equal(1, death.WarpToSaveCalls);
    }

    [Fact]
    public void Teleport_on_noteleport_map_refuses()
    {
        var pos = new StubPositions();
        var death = new StubDeath();
        var svc = NewService(pos, death, noTeleport: true);
        var pc = NewPc(mapId: (uint)"arena".GetHashCode());

        Assert.False(svc.CastEndMap(pc, "Random", SkillIds.AL_TELEPORT));
        Assert.Equal(0, pos.RandomWarpCalls);
        Assert.Equal(0, death.WarpToSaveCalls);
    }

    [Fact]
    public void Mob_caster_does_not_warp()
    {
        var pos = new StubPositions();
        var svc = NewService(pos, new StubDeath(), noTeleport: false);
        var mob = new MobEntity(new EntityId(5), 1002, "Poring", (uint)"prt".GetHashCode(), 0, 0);

        Assert.False(svc.CastEndMap(mob, "Random", SkillIds.AL_TELEPORT));
        Assert.Equal(0, pos.RandomWarpCalls);
    }

    // ---- helpers ----

    private static PlayerEntity NewPc(uint mapId)
        => new(1, 1, "Aco", Guid.NewGuid(), mapId, 50, 50);

    private static SkillCastEndService NewService(StubPositions pos, StubDeath death, bool noTeleport)
    {
        var map = new MapData(noTeleport ? "arena" : "prt", 100, 100, new byte[100 * 100]);
        return new SkillCastEndService(
            new SkillDb(),
            new SkillResolverRegistry(Array.Empty<ISkillResolver>()),
            new NoOpUnits(),
            NullLogger<SkillCastEndService>.Instance,
            positions: pos,
            death: death,
            mapFlags: new StubFlags(noTeleport ? map.Name : null),
            maps: new StubWorld(map));
    }

    private sealed class StubPositions : IPlayerPositionHelpers
    {
        public int RandomWarpCalls;
        public bool RandomWarp(PlayerEntity pc) { RandomWarpCalls++; return true; }
        public bool IsLastpointSpecial(string mapName) => false;
        public bool Memo(PlayerEntity pc, int slot) => false;
        public bool IsBasilicaCell(PlayerEntity pc) => false;
    }

    private sealed class StubDeath : IPcDeathService
    {
        public int WarpToSaveCalls;
        public bool WarpToSavepoint(PlayerEntity pc) { WarpToSaveCalls++; return true; }
        public void OnPcDead(PlayerEntity pc, Entity? source) { }
        public void Respawn(PlayerEntity pc) { }
        public bool IsDead(PlayerEntity pc) => false;
        public void SetSavepoint(int characterId, string mapName, short x, short y) { }
    }

    private sealed class StubFlags : IMapFlagService
    {
        private readonly string? _noTeleportMap;
        public StubFlags(string? noTeleportMap) => _noTeleportMap = noTeleportMap;
        public bool IsSet(string mapName, MapFlag flag)
            => flag == MapFlag.NoTeleport && _noTeleportMap != null
               && string.Equals(mapName, _noTeleportMap, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly MapData _map;
        public StubWorld(MapData map) => _map = map;
        public MapData? Get(string name) => string.Equals(name, _map.Name, StringComparison.OrdinalIgnoreCase) ? _map : null;
        public IEnumerable<MapData> All => new[] { _map };
        public int TotalCells => _map.CellCount;
        public bool Contains(string name) => string.Equals(name, _map.Name, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NoOpUnits : ISkillUnitService
    {
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
        public void DelUnitGroup(SkillUnitGroup group) { }
        public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius) => Array.Empty<SkillUnit>();
        public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius, ushort skillId) => Array.Empty<SkillUnit>();
    }
}
