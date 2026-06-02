using System;
using System.Collections.Generic;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Persistence;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-52 — die_counter persistence (the PC_DIE_COUNTER char register) + the
/// on-death increment that recalcs so a Super Novice's never-died all-stat +10
/// drops on the first death. rAthena pc_dead → pc_setparam(SP_PCDIECOUNTER, +1).
/// </summary>
public class Combat52DieCounterTests
{
    private const int SuperNovice = 23;

    // ---- persistence glue (DieCounterReg) ----

    [Fact]
    public void DieCounterReg_round_trips_through_the_perm_scope()
    {
        var regs = PlayerVarRegs.Empty();
        Assert.Equal(0, DieCounterReg.Read(regs)); // absent → 0

        DieCounterReg.Persist(regs, 3);
        Assert.Equal(3, DieCounterReg.Read(regs));
    }

    [Fact]
    public void DieCounterReg_leaves_zero_absent()
    {
        var regs = PlayerVarRegs.Empty();
        DieCounterReg.Persist(regs, 0);
        Assert.False(regs.Perm.Bag.ContainsKey(DieCounterReg.VarName)); // rAthena drops 0 regs
        Assert.Equal(0, DieCounterReg.Read(regs));
    }

    [Fact]
    public void DieCounterReg_read_handles_null_regs()
        => Assert.Equal(0, DieCounterReg.Read(null));

    // ---- on-death increment + recalc ----

    [Fact]
    public void OnPcDead_increments_die_counter_and_drops_supernovice_bonus()
    {
        var (death, calc, entities) = Build();
        var pc = NewSuperNovice();
        entities.Add(pc);

        // Initial recalc: never died → +10 all stats.
        calc.CalcPc(pc, PcRecalcInputs.FromCurrent(pc));
        var aliveStr = pc.Stats.Str;

        death.OnPcDead(pc, source: null);

        Assert.Equal(1, pc.DieCounter);
        Assert.Equal(aliveStr - 10, pc.Stats.Str); // recalc dropped the never-died +10
    }

    [Fact]
    public void OnPcDead_is_idempotent_per_death()
    {
        var (death, calc, entities) = Build();
        var pc = NewSuperNovice();
        entities.Add(pc);
        calc.CalcPc(pc, PcRecalcInputs.FromCurrent(pc));

        death.OnPcDead(pc, null);
        death.OnPcDead(pc, null); // already dead → no second increment
        Assert.Equal(1, pc.DieCounter);
    }

    // ---- helpers ----

    private static PlayerEntity NewSuperNovice()
    {
        var pc = new PlayerEntity(1, 1, "Nov", Guid.NewGuid(), 0, 50, 50) { ClassId = SuperNovice };
        pc.Level = 99;
        pc.JobLevel = 70; // gate: joblv >= 70
        pc.BaseParams.Str = pc.BaseParams.Agi = pc.BaseParams.Vit = 1;
        pc.BaseParams.IntStat = pc.BaseParams.Dex = pc.BaseParams.Luk = 1;
        return pc;
    }

    private static (PcDeathService death, StatusCalcService calc, EntityRegistry entities) Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 100, 100, new byte[100 * 100]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var visibility = new VisibilityService(entities, new RecordingDispatcher());
        var calc = new StatusCalcService();
        var death = new PcDeathService(new StubServices(calc), visibility, NullLogger<PcDeathService>.Instance);
        return (death, calc, entities);
    }

    private sealed class StubServices : IServiceProvider
    {
        private readonly IStatusCalcService _calc;
        public StubServices(IStatusCalcService calc) => _calc = calc;
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IStatusCalcService)) return _calc;
            if (serviceType == typeof(IAttackStopper)) return new NoOpAttackStopper();
            return null;
        }
    }

    private sealed class NoOpAttackStopper : IAttackStopper
    {
        public void StopAttack(Entity source) { }
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
}
