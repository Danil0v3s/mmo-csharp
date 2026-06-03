using System;
using System.Collections.Generic;
using System.Linq;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Handlers;
using Map.Server.Movement;
using Map.Server.Session;
using Map.Server.Tests.Session;
using Map.Server.Skills;
using Map.Server.Skills.Units;
using Map.Server.Skills.Units.Handlers;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-67 — the Warp Portal ground unit (warps steppers to the stored exit) + the
/// <c>pc_memo</c> set-path (gates + insert-at-0) reachable via CZ_REMEMBER_WARPPOINT.
/// </summary>
public class Combat67WarpPortalMemoTests
{
    // ---- Warp Portal ground unit ----

    [Fact]
    public void Portal_warps_a_stepper_to_the_stored_exit()
    {
        var setpos = new RecSetpos();
        var portal = new WarpPortalUnit(setpos);
        var group = new SkillUnitGroup
        {
            SkillId = SkillIds.AL_WARP, SkillLevel = 4, CasterId = new EntityId(1),
            MapId = 0, ExpiresAt = 0, IntervalMs = 1000,
            DestMap = "geffen", DestX = 100, DestY = 120,
        };
        var stepper = new PlayerEntity(2, 2, "Stepper", Guid.NewGuid(), 0, 0, 0) { Hp = 100, MaxHp = 100 };

        portal.OnPlace(caster: null, stepper, skillLevel: 4, tick: 0, new StubCtx(), group);

        Assert.Equal(1, setpos.Calls);
        Assert.Equal("geffen", setpos.Map);
        Assert.Equal((short)100, setpos.X);
        Assert.Equal((short)120, setpos.Y);
    }

    [Fact]
    public void Portal_does_not_warp_mobs_and_is_inert_without_a_destination()
    {
        var setpos = new RecSetpos();
        var portal = new WarpPortalUnit(setpos);
        var mob = new MobEntity(new EntityId(5), 1002, "Poring", 0, 0, 0) { Hp = 100 };
        var withDest = new SkillUnitGroup
        {
            SkillId = SkillIds.AL_WARP, SkillLevel = 1, CasterId = new EntityId(1),
            MapId = 0, ExpiresAt = 0, IntervalMs = 1000, DestMap = "geffen", DestX = 1, DestY = 1,
        };
        // A mob on the portal is not warped.
        portal.OnPlace(null, mob, 1, 0, new StubCtx(), withDest);
        Assert.Equal(0, setpos.Calls);

        // A player on a destination-less portal is inert.
        var pc = new PlayerEntity(2, 2, "P", Guid.NewGuid(), 0, 0, 0) { Hp = 100 };
        var noDest = new SkillUnitGroup
        {
            SkillId = SkillIds.AL_WARP, SkillLevel = 1, CasterId = new EntityId(1),
            MapId = 0, ExpiresAt = 0, IntervalMs = 1000,
        };
        portal.OnPlace(null, pc, 1, 0, new StubCtx(), noDest);
        Assert.Equal(0, setpos.Calls);
    }

    [Fact]
    public void Portal_handler_values_match_rathena()
    {
        var h = new WarpPortalUnit();
        Assert.Equal(SkillIds.AL_WARP, h.SkillId);
        Assert.Equal(10_000, h.DurationMs(1)); // 5000 + 5000*1
        Assert.Equal(25_000, h.DurationMs(4)); // 5000 + 5000*4
        Assert.Equal(0, h.Radius(1));          // single cell
        // The caster is warped too (Warp Portal does not exclude its owner).
        Assert.True(h.IsValidVictim(new PlayerEntity(1, 1, "C", Guid.NewGuid(), 0, 0, 0) { Hp = 1 },
                                    new PlayerEntity(1, 1, "C", Guid.NewGuid(), 0, 0, 0) { Hp = 1 }));
    }

    // ---- pc_memo set-path ----

    [Fact]
    public void Memo_inserts_at_slot_zero_and_shifts_the_list()
    {
        var (helpers, world) = NewHelpers();
        var pc = NewWarper(4);

        pc.MapId = MapId("prontera");
        Assert.True(helpers.Memo(pc, -1));
        Assert.Equal("prontera", pc.MemoPoints[0].MapName);

        pc.MapId = MapId("geffen");
        Assert.True(helpers.Memo(pc, -1));
        Assert.Equal("geffen", pc.MemoPoints[0].MapName);   // newest at slot 0
        Assert.Equal("prontera", pc.MemoPoints[1].MapName); // shifted down
    }

    [Fact]
    public void Memo_dedups_an_already_remembered_map_to_slot_zero()
    {
        var (helpers, _) = NewHelpers();
        var pc = NewWarper(4);

        pc.MapId = MapId("prontera"); helpers.Memo(pc, -1);
        pc.MapId = MapId("geffen");   helpers.Memo(pc, -1);
        // Re-memo prontera → it moves to slot 0; geffen shifts to slot 1; no duplicate.
        pc.MapId = MapId("prontera"); Assert.True(helpers.Memo(pc, -1));
        Assert.Equal("prontera", pc.MemoPoints[0].MapName);
        Assert.Equal("geffen", pc.MemoPoints[1].MapName);
    }

    [Fact]
    public void Memo_refused_on_a_nomemo_map()
    {
        var (helpers, _) = NewHelpers(noMemoMap: "prontera");
        var pc = NewWarper(4);
        pc.MapId = MapId("prontera");
        Assert.False(helpers.Memo(pc, -1));
        Assert.True(string.IsNullOrEmpty(pc.MemoPoints[0].MapName));
    }

    [Fact]
    public void Memo_refused_below_al_warp_level_two()
    {
        var (helpers, _) = NewHelpers();
        var pc = NewWarper(1); // lv 1 → cannot memo (needs ≥2)
        pc.MapId = MapId("prontera");
        Assert.False(helpers.Memo(pc, -1));

        var unlearned = NewWarper(0);
        unlearned.MapId = MapId("prontera");
        Assert.False(helpers.Memo(unlearned, -1));
    }

    [Fact]
    public void RememberWarpPoint_handler_routes_to_memo_slot_minus_one()
    {
        const string mapName = "prontera";
        var (helpers, _) = NewHelpers();
        var world = new StubWorld(MapsOf(mapName, "geffen", "payon"));
        var entities = new EntityRegistry(world);
        var pc = NewWarper(4);
        pc.MapId = MapId(mapName);
        entities.Add(pc);

        var handler = new RememberWarpPointHandler(entities, helpers, NullLogger<RememberWarpPointHandler>.Instance);
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id,
        };

        handler.HandleAsync(session, new CZ_REMEMBER_WARPPOINT()).GetAwaiter().GetResult();
        Assert.Equal(mapName, pc.MemoPoints[0].MapName);
    }

    // ---- helpers ----

    private static uint MapId(string name) => (uint)name.GetHashCode();

    private static PlayerEntity NewWarper(int alWarpLevel)
    {
        var pc = new PlayerEntity(1, 1, "Aco", Guid.NewGuid(), 0, 50, 50);
        if (alWarpLevel > 0) pc.LearnedSkills[SkillIds.AL_WARP] = (byte)alWarpLevel;
        return pc;
    }

    private static MapData[] MapsOf(params string[] names)
        => names.Select(n => new MapData(n, 100, 100, new byte[100 * 100])).ToArray();

    private static (PlayerPositionHelpers helpers, StubWorld world) NewHelpers(string? noMemoMap = null)
    {
        var world = new StubWorld(MapsOf("prontera", "geffen", "payon"));
        var helpers = new PlayerPositionHelpers(
            world, new RecSetpos(), new RecordingStatusChangeService(new SkillTraceRecorder()),
            NullLogger<PlayerPositionHelpers>.Instance, new StubFlags(noMemoMap));
        return (helpers, world);
    }

    private sealed class RecSetpos : IPcSetposService
    {
        public int Calls; public string? Map; public short X, Y;
        public SetposResult Setpos(PlayerEntity pc, string mapName, short x, short y)
        {
            Calls++; Map = mapName; X = x; Y = y; return SetposResult.Ok;
        }
    }

    private sealed class StubFlags : IMapFlagService
    {
        private readonly string? _noMemo;
        public StubFlags(string? noMemo) => _noMemo = noMemo;
        public bool IsSet(string mapName, MapFlag flag)
            => flag == MapFlag.NoMemo && string.Equals(mapName, _noMemo, StringComparison.OrdinalIgnoreCase);
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

    private sealed class StubCtx : ISkillUnitContext
    {
        public IDamageService Damage => null!;
        public IStatusChangeService? Sc => null;
        public ISkillClientService? Client => null;
    }
}
