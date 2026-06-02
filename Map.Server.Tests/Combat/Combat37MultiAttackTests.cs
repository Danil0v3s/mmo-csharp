using System;
using System.Linq;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-37 — auto-attack <c>battle_calc_multi_attack</c> branches beyond the
/// COMBAT-17 double-attack: SC_FEARBREEZE (bow, ammo-capped tier roll,
/// battle.cpp:4403) and Gunslinger Chain Action (revolver + GS_CHAINACTION or
/// SC_E_CHAIN, battle.cpp:4459, starts SC_QD_SHOT_READY).
/// </summary>
public class Combat37MultiAttackTests
{
    // ---- Fear Breeze ----

    [Fact]
    public void FearBreeze_val5_low_roll_fires_five_hits()
    {
        var (calc, pc, _) = Setup(WeaponTypeCodes.Bow, fearBreezeVal1: 5, ammo: 10,
            rng: new SeqRandom(hitRoll: 0, multiRoll: 0)); // chance 0 < 4 → 5 hits
        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(5, dmg.Hits);
        Assert.Equal(250, dmg.Damage);                  // 50 × 5
        Assert.Equal(DamageActionType.MultiHit, dmg.Type);
    }

    [Fact]
    public void FearBreeze_tier_ladder_picks_four_hits_in_the_4_to_6_window()
    {
        var (calc, pc, _) = Setup(WeaponTypeCodes.Bow, fearBreezeVal1: 5, ammo: 10,
            rng: new SeqRandom(hitRoll: 0, multiRoll: 5)); // 4<=5<7 → 4 hits
        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(4, dmg.Hits);
    }

    [Fact]
    public void FearBreeze_div_is_capped_by_ammo_amount()
    {
        // val5 low roll would be 5 hits, but only 3 arrows are equipped.
        var (calc, pc, _) = Setup(WeaponTypeCodes.Bow, fearBreezeVal1: 5, ammo: 3,
            rng: new SeqRandom(hitRoll: 0, multiRoll: 0));
        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(3, dmg.Hits);
    }

    [Fact]
    public void FearBreeze_records_div_minus_one_in_val4()
    {
        var (calc, pc, sc) = Setup(WeaponTypeCodes.Bow, fearBreezeVal1: 5, ammo: 10,
            rng: new SeqRandom(hitRoll: 0, multiRoll: 0));
        calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(4, sc.Get(pc, StatusType.Fearbreeze)!.Val4); // div(5) - 1
    }

    [Fact]
    public void FearBreeze_requires_a_bow()
    {
        var (calc, pc, _) = Setup(WeaponTypeCodes.OneHandSword, fearBreezeVal1: 5, ammo: 10,
            rng: new SeqRandom(hitRoll: 0, multiRoll: 0));
        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(1, dmg.Hits);
    }

    [Fact]
    public void FearBreeze_needs_more_than_one_round()
    {
        var (calc, pc, _) = Setup(WeaponTypeCodes.Bow, fearBreezeVal1: 5, ammo: 1,
            rng: new SeqRandom(hitRoll: 0, multiRoll: 0));
        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(1, dmg.Hits);
    }

    // ---- Chain Action ----

    [Fact]
    public void ChainAction_revolver_with_learned_skill_fires_two_hits_and_starts_qd_shot()
    {
        var rec = new SkillTraceRecorder();
        var sc = new RecordingStatusChangeService(rec);
        var pc = MakeSwinger(WeaponTypeCodes.Revolver, 50);
        pc.LearnedSkills[Map.Server.Skills.SkillIds.GS_CHAINACTION] = 5; // 5*5 = 25%
        var calc = new BattleCalculator(new SeqRandom(0, 0), sc: sc, ammo: new FakeAmmo(10)); // roll 0 < 25

        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(2, dmg.Hits);
        Assert.Equal(100, dmg.Damage);
        Assert.Contains(rec.Events, e => e.Kind == "sc-start"
            && (string)e.Data["type"]! == StatusType.QdShotReady.ToString());
    }

    [Fact]
    public void ChainAction_via_eternal_chain_sc()
    {
        var rec = new SkillTraceRecorder();
        var sc = new RecordingStatusChangeService(rec);
        var pc = MakeSwinger(WeaponTypeCodes.Revolver, 50);
        sc.Start(pc, StatusType.EChain, 5, 0, 0, 0, 60_000); // E_Chain val1 = 5 → 25%
        var calc = new BattleCalculator(new SeqRandom(0, 0), sc: sc, ammo: new FakeAmmo(10));

        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(2, dmg.Hits);
    }

    [Fact]
    public void ChainAction_wrong_weapon_does_not_proc()
    {
        var rec = new SkillTraceRecorder();
        var sc = new RecordingStatusChangeService(rec);
        var pc = MakeSwinger(WeaponTypeCodes.Dagger, 50);
        pc.LearnedSkills[Map.Server.Skills.SkillIds.GS_CHAINACTION] = 5; // learned but not a revolver
        var calc = new BattleCalculator(new SeqRandom(0, 0), sc: sc, ammo: new FakeAmmo(10));

        var dmg = calc.CalcWeaponAttack(pc, MakeTarget());
        Assert.Equal(1, dmg.Hits);
    }

    // ---- helpers ----

    private static (BattleCalculator calc, PlayerEntity pc, RecordingStatusChangeService sc) Setup(
        int weapon, int fearBreezeVal1, int ammo, SeqRandom rng)
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeSwinger(weapon, 50);
        if (fearBreezeVal1 > 0) sc.Start(pc, StatusType.Fearbreeze, fearBreezeVal1, 0, 0, 0, 60_000);
        var calc = new BattleCalculator(rng, sc: sc, ammo: new FakeAmmo(ammo));
        return (calc, pc, sc);
    }

    private static PlayerEntity MakeSwinger(int weapon, int swing)
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = weapon };
        pc.Stats.Dex = (short)swing;       // PC atkmin is DEX-derived; pin → deterministic swing
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = (ushort)swing;
        pc.Stats.WatkMax = (ushort)swing;
        pc.Stats.Batk = 0;
        pc.Stats.Cri = 0;                  // no critical
        pc.Stats.Hit = 10000;              // always hits
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium;
        m.Stats.Flee = 0; m.Stats.Flee2 = 0;
        return m;
    }

    private sealed class SeqRandom : Random
    {
        private readonly int[] _values;
        private int _i;
        public SeqRandom(int hitRoll, int multiRoll) => _values = new[] { hitRoll, multiRoll };
        public override int Next(int maxValue) => _i < _values.Length ? _values[_i++] : 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class FakeAmmo : IAmmoService
    {
        private readonly int _count;
        public FakeAmmo(int count) => _count = count;
        public bool HasUsableAmmo(PlayerEntity pc) => _count > 0;
        public bool ConsumeAmmo(PlayerEntity pc) => true;
        public int GetEquippedAmmoAmount(PlayerEntity pc) => _count;
    }
}
