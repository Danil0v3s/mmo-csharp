using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-43 — cardfix remainder, the ignore-def slice. <c>bonus bIgnoreDefRace,RC_X</c>
/// / <c>bonus bIgnoreDefClass,Class_X</c> skip the DEF-reduction stage vs the carded
/// race/class (rAthena battle.cpp:3379). (Element-debuff / race2 / distinct magic
/// arrays are COMBAT-63.)
/// </summary>
public class Combat43CardfixRemainderTests
{
    // ---- extractor: the constant-arg bonus form ----

    [Fact]
    public void Extractor_parses_ignore_def_race_constant()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus bIgnoreDefRace,RC_DemiHuman;", b);

        Assert.True((b.IgnoreDefRace & (1 << (int)BattleRace.Demihuman)) != 0);
        Assert.Equal(0, b.IgnoreDefRace & (1 << (int)BattleRace.Brute)); // only the carded race
    }

    [Fact]
    public void Extractor_rc_all_sets_every_real_race()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus bIgnoreDefRace,RC_All;", b);

        Assert.True((b.IgnoreDefRace & (1 << (int)BattleRace.Demihuman)) != 0);
        Assert.True((b.IgnoreDefRace & (1 << (int)BattleRace.Dragon)) != 0);
        Assert.True((b.IgnoreDefRace & (1 << (int)BattleRace.Formless)) != 0);
    }

    [Fact]
    public void Extractor_parses_ignore_def_class_constant()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus bIgnoreDefClass,Class_Boss;", b);
        Assert.True((b.IgnoreDefClass & (1 << (int)BattleClassFlag.Boss)) != 0);
    }

    // ---- def stage: ignore-def skips the DEF subtract ----

    [Fact]
    public void Ignore_def_race_skips_the_def_subtract()
    {
        var target = HighDefTarget(BattleRace.Demihuman, boss: false);

        var plain = MakeSwinger();
        var plainDmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(plain, target).Damage;

        var piercer = MakeSwinger();
        piercer.EquipBonuses.IgnoreDefRace = 1 << (int)BattleRace.Demihuman;
        var pierceDmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(piercer, HighDefTarget(BattleRace.Demihuman, boss: false)).Damage;

        Assert.True(pierceDmg > plainDmg,
            $"ignore-def ({pierceDmg}) should out-damage the def-reduced swing ({plainDmg})");
    }

    [Fact]
    public void Ignore_def_race_does_not_fire_against_a_different_race()
    {
        var target = HighDefTarget(BattleRace.Brute, boss: false);

        var plainDmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(MakeSwinger(), target).Damage;

        var piercer = MakeSwinger();
        piercer.EquipBonuses.IgnoreDefRace = 1 << (int)BattleRace.Demihuman; // carded vs DemiHuman, target is Brute
        var pierceDmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(piercer, HighDefTarget(BattleRace.Brute, boss: false)).Damage;

        Assert.Equal(plainDmg, pierceDmg); // def still subtracted
    }

    [Fact]
    public void Ignore_def_class_skips_def_against_a_boss()
    {
        var plainDmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(MakeSwinger(), HighDefTarget(BattleRace.Demihuman, boss: true)).Damage;

        var piercer = MakeSwinger();
        piercer.EquipBonuses.IgnoreDefClass = 1 << (int)BattleClassFlag.Boss;
        var pierceDmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(piercer, HighDefTarget(BattleRace.Demihuman, boss: true)).Damage;

        Assert.True(pierceDmg > plainDmg);
    }

    // ---- helpers ----

    private static PlayerEntity MakeSwinger()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Dex = 200;          // pin atkmin == atkmax → deterministic swing
        pc.Stats.WatkMin = pc.Stats.WatkMax = 200;
        pc.Stats.Batk = 0;
        pc.Stats.Cri = 0;
        pc.Stats.Hit = 10000;
        pc.Stats.WeaponLevel = 0;
        return pc;
    }

    private static MobEntity HighDefTarget(BattleRace race, bool boss)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "TARGET", Name = "Target", Hp = 50000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.MaxHp = m.Hp = 50000;
        m.Stats.Def = 100; m.Stats.Def2 = 80;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium;
        m.Stats.Race = race;
        m.Stats.Flee = 0; m.Stats.Flee2 = 0;
        if (boss) m.Stats.Mode |= MobMode.StatusImmune;
        return m;
    }
}
