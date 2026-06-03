using System;
using System.Collections.Generic;
using System.IO;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Handlers;
using Map.Server.Movement;
using Map.Server.Session;
using Map.Server.Skills;
using Map.Server.Skills.Resolvers;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-48 — AL_WARP destination resolution + CZ_SELECT_WARPPOINT routing.
/// rAthena skill_castend_map AL_WARP: resolve the picked map among the
/// level-gated [save_point, memo_point[0..2]] list, gate on nowarp/nowarpto,
/// then warp. The CZ_SELECT_WARPPOINT handler routes the client's pick in.
/// </summary>
public class Combat48WarpTests
{
    [Fact]
    public void Warp_to_memo_destination_places_a_portal()
    {
        // COMBAT-67 — AL_WARP no longer warps the caster directly; it places a Warp Portal
        // ground unit at the cast cell stamped with the chosen exit.
        var setpos = new StubSetpos();
        var units = new NoOpUnits();
        var svc = NewService(setpos, new StubDeath(), srcMap: "prt", units: units);
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 4;
        pc.MemoPoints[0] = ("geffen", 100, 120);

        Assert.True(svc.CastEndMap(pc, "geffen", SkillIds.AL_WARP));
        Assert.Equal(0, setpos.Calls); // no direct warp at cast time
        Assert.NotNull(units.LastPlaced);
        Assert.Equal(SkillIds.AL_WARP, units.LastPlaced!.SkillId);
        Assert.Equal("geffen", units.LastPlaced.DestMap);
        Assert.Equal((short)100, units.LastPlaced.DestX);
        Assert.Equal((short)120, units.LastPlaced.DestY);
    }

    [Fact]
    public void Warp_savepoint_warps_to_save()
    {
        var death = new StubDeath();
        var svc = NewService(new StubSetpos(), death, srcMap: "prt");
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 1;

        Assert.True(svc.CastEndMap(pc, "SavePoint", SkillIds.AL_WARP));
        Assert.Equal(1, death.WarpToSaveCalls);
    }

    [Fact]
    public void Warp_to_nowarpto_map_refuses()
    {
        var setpos = new StubSetpos();
        var svc = NewService(setpos, new StubDeath(), srcMap: "prt", noWarpToMap: "gefenia");
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 4;
        pc.MemoPoints[0] = ("gefenia", 50, 60);

        Assert.False(svc.CastEndMap(pc, "gefenia", SkillIds.AL_WARP));
        Assert.Equal(0, setpos.Calls);
    }

    [Fact]
    public void Warp_from_nowarp_source_refuses()
    {
        var setpos = new StubSetpos();
        var svc = NewService(setpos, new StubDeath(), srcMap: "jail", noWarpMap: "jail");
        var pc = NewPc(mapId: (uint)"jail".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 4;
        pc.MemoPoints[0] = ("geffen", 100, 120);

        Assert.False(svc.CastEndMap(pc, "geffen", SkillIds.AL_WARP));
        Assert.Equal(0, setpos.Calls);
    }

    [Fact]
    public void Warp_above_level_gate_refuses()
    {
        // memo slot 1 (memo_point[1]) is only reachable at AL_WARP lv >= 3.
        var setpos = new StubSetpos();
        var svc = NewService(setpos, new StubDeath(), srcMap: "prt");
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 2;
        pc.MemoPoints[1] = ("payon", 80, 90);

        Assert.False(svc.CastEndMap(pc, "payon", SkillIds.AL_WARP));
        Assert.Equal(0, setpos.Calls);
    }

    [Fact]
    public void Warp_cancel_aborts()
    {
        var setpos = new StubSetpos();
        var svc = NewService(setpos, new StubDeath(), srcMap: "prt");
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 4;
        pc.MemoPoints[0] = ("geffen", 100, 120);

        Assert.False(svc.CastEndMap(pc, "cancel", SkillIds.AL_WARP));
        Assert.Equal(0, setpos.Calls);
    }

    [Fact]
    public void Warp_unknown_destination_refuses()
    {
        var setpos = new StubSetpos();
        var svc = NewService(setpos, new StubDeath(), srcMap: "prt");
        var pc = NewPc(mapId: (uint)"prt".GetHashCode());
        pc.LearnedSkills[SkillIds.AL_WARP] = 4;
        pc.MemoPoints[0] = ("geffen", 100, 120);

        Assert.False(svc.CastEndMap(pc, "morocc", SkillIds.AL_WARP));
        Assert.Equal(0, setpos.Calls);
    }

    [Fact]
    public void Handler_routes_pick_to_CastEndMap()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var pc = new PlayerEntity(1, 1, "Aco", Guid.NewGuid(), (uint)mapName.GetHashCode(), 50, 50);
        entities.Add(pc);

        var castEnd = new RecordingCastEnd();
        var handler = new SelectWarpPointHandler(entities, castEnd, NullLogger<SelectWarpPointHandler>.Instance);

        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(
            sockets.ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry,
            NullLogger.Instance)
        {
            AccountId = 1,
            CharacterId = 1,
            AuthState = MapAuthState.Spawned,
            EntityId = pc.Id,
        };

        var packet = BuildPacket(SkillIds.AL_WARP, "geffen");
        handler.HandleAsync(session, packet).GetAwaiter().GetResult();

        Assert.Equal(1, castEnd.Calls);
        Assert.Same(pc, castEnd.Source);
        Assert.Equal("geffen", castEnd.Map);
        Assert.Equal(SkillIds.AL_WARP, castEnd.Skill);
    }

    // ---- helpers ----

    private static CZ_SELECT_WARPPOINT BuildPacket(ushort skillId, string mapName)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(skillId);
        var name = new byte[16];
        var raw = System.Text.Encoding.ASCII.GetBytes(mapName);
        Array.Copy(raw, name, Math.Min(raw.Length, name.Length));
        bw.Write(name);
        bw.Flush();
        ms.Position = 0;
        using var br = new BinaryReader(ms);
        return CZ_SELECT_WARPPOINT.Create(br);
    }

    private static PlayerEntity NewPc(uint mapId)
        => new(1, 1, "Aco", Guid.NewGuid(), mapId, 50, 50);

    private static SkillCastEndService NewService(
        StubSetpos setpos, StubDeath death, string srcMap,
        string? noWarpToMap = null, string? noWarpMap = null, ISkillUnitService? units = null)
    {
        var map = new MapData(srcMap, 100, 100, new byte[100 * 100]);
        return new SkillCastEndService(
            new SkillDb(),
            new SkillResolverRegistry(Array.Empty<ISkillResolver>()),
            units ?? new NoOpUnits(),
            NullLogger<SkillCastEndService>.Instance,
            positions: null,
            death: death,
            mapFlags: new StubFlags(noWarpToMap, noWarpMap),
            maps: new StubWorld(map),
            setpos: setpos);
    }

    private sealed class StubSetpos : IPcSetposService
    {
        public int Calls;
        public string? Map;
        public short X, Y;
        public SetposResult Result = SetposResult.Ok;
        public SetposResult Setpos(PlayerEntity pc, string mapName, short x, short y)
        {
            Calls++; Map = mapName; X = x; Y = y;
            return Result;
        }
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
        private readonly string? _noWarpTo;
        private readonly string? _noWarp;
        public StubFlags(string? noWarpTo, string? noWarp) { _noWarpTo = noWarpTo; _noWarp = noWarp; }
        public bool IsSet(string mapName, MapFlag flag)
            => (flag == MapFlag.NoWarpTo && _noWarpTo != null && string.Equals(mapName, _noWarpTo, StringComparison.OrdinalIgnoreCase))
            || (flag == MapFlag.NoWarp && _noWarp != null && string.Equals(mapName, _noWarp, StringComparison.OrdinalIgnoreCase));
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

    private sealed class RecordingCastEnd : ISkillCastEndService
    {
        public int Calls;
        public Entity? Source;
        public string? Map;
        public ushort Skill;
        public bool CastEndMap(Entity source, string targetMap, ushort skillId)
        {
            Calls++; Source = source; Map = targetMap; Skill = skillId;
            return true;
        }
        public bool CastEndDamageId(Entity source, Entity target, ushort skillId, ushort skillLevel) => false;
        public bool CastEndNoDamageId(Entity source, Entity target, ushort skillId, ushort skillLevel) => false;
        public bool CastEndPos2(Entity source, short x, short y, ushort skillId, ushort skillLevel) => false;
    }

    private sealed class NoOpUnits : ISkillUnitService
    {
        // COMBAT-67 — capture the placed group so the AL_WARP portal-placement path is testable.
        public SkillUnitGroup? LastPlaced;
        public SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short cx, short cy)
            => Place(caster, skillId, skillLevel, cx, cy, 0);
        public SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short cx, short cy, int delayMs)
        {
            LastPlaced = new SkillUnitGroup
            {
                SkillId = skillId, SkillLevel = skillLevel, CasterId = caster.Id,
                MapId = caster.MapId, ExpiresAt = 0, IntervalMs = 1000,
            };
            return LastPlaced;
        }
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
