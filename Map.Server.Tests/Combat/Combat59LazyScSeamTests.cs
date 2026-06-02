using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-59 — the lazy <see cref="IStatusChangeService"/> seam that lets the
/// production <see cref="BattleCalculator"/> read SCs without forming the
/// construction cycle IStatusChangeService → IDamageService → IBattleCalculator.
/// The lazy resolves on the first combat SC read (well after startup), so SC-gated
/// combat reads (Maximize Power, Fear Breeze, EDP, …) go live.
/// </summary>
public class Combat59LazyScSeamTests
{
    [Fact]
    public void Lazy_sc_is_not_resolved_at_construction_but_is_on_first_combat_read()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var calls = 0;
        var calc = new BattleCalculator(
            rng: new Random(0),
            scLazy: new Lazy<IStatusChangeService>(() => { calls++; return sc; }));

        // No resolution at construction → no DI cycle at startup.
        Assert.Equal(0, calls);

        calc.CalcWeaponAttack(MakeSwinger(), MakeTarget());
        Assert.Equal(1, calls); // resolved on the first SC read

        calc.CalcWeaponAttack(MakeSwinger(), MakeTarget());
        Assert.Equal(1, calls); // Lazy caches — resolved once
    }

    [Fact]
    public void Lazy_sc_drives_the_same_effect_as_an_explicit_sc()
    {
        // SC_MAXIMIZEPOWER forces the weapon roll to its max (atkMax), so with
        // WatkMin 10 / WatkMax 100 it lifts damage above the rng-min baseline.
        var lazyDmg = DamageWith(useLazy: true, maximize: true);
        var explicitDmg = DamageWith(useLazy: false, maximize: true);
        var baseline = DamageWith(useLazy: true, maximize: false);

        Assert.Equal(explicitDmg, lazyDmg);   // lazy path == explicit path
        Assert.True(lazyDmg > baseline);       // Maximize Power actually fired via the lazy seam
    }

    // ---- helpers ----

    private static long DamageWith(bool useLazy, bool maximize)
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeSwinger();
        if (maximize) sc.Start(pc, StatusType.Maximizepower, 1, 0, 0, 0, 60_000);
        var calc = useLazy
            ? new BattleCalculator(new MinRandom(), scLazy: new Lazy<IStatusChangeService>(() => sc))
            : new BattleCalculator(new MinRandom(), sc: sc);
        return calc.CalcWeaponAttack(pc, MakeTarget()).Total;
    }

    private static PlayerEntity MakeSwinger()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = 0 };
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = 10; pc.Stats.WatkMax = 100; // spread so force-max-roll matters
        pc.Stats.Batk = 0; pc.Stats.Cri = 0; pc.Stats.Hit = 10000;
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0;
        return m;
    }

    /// <summary>Always rolls the minimum — so a non-maximized swing uses WatkMin.</summary>
    private sealed class MinRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
