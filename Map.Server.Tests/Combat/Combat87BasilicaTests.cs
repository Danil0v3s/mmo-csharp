using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-87 — renewal SC_BASILICA: the offensive element buff (weapon +val1*5% vs Dark/Undead,
/// Holy magic +val1*3%, status.cpp:4768) and the NoAttack caster state.
/// </summary>
public class Combat87BasilicaTests
{
    // ---- weapon addele[Dark/Undead] += val1*5 (read live in CalcCardFix) ----

    [Fact]
    public void Basilica_weapon_buff_adds_to_dark_and_undead_targets()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var cards = new BattleCardService(NullLogger<BattleCardService>.Instance, new Lazy<IStatusChangeService>(() => sc));
        var attacker = NewPlayer();
        sc.Start(attacker, StatusType.Basilica, val1: 5, 0, 0, 0, durationMs: 30_000, attacker); // +25%

        Assert.Equal(1250, cards.CalcCardFix(BattleAttackType.Weapon, attacker, MakeMob(BattleElement.Dark), 1000, false));
        Assert.Equal(1250, cards.CalcCardFix(BattleAttackType.Weapon, attacker, MakeMob(BattleElement.Undead), 1000, false));
        // not a Dark/Undead target → no buff.
        Assert.Equal(1000, cards.CalcCardFix(BattleAttackType.Weapon, attacker, MakeMob(BattleElement.Neutral), 1000, false));
    }

    [Fact]
    public void Basilica_weapon_buff_clears_when_the_sc_ends()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var cards = new BattleCardService(NullLogger<BattleCardService>.Instance, new Lazy<IStatusChangeService>(() => sc));
        var attacker = NewPlayer();
        var dark = MakeMob(BattleElement.Dark);

        sc.Start(attacker, StatusType.Basilica, val1: 5, 0, 0, 0, durationMs: 30_000, attacker);
        Assert.Equal(1250, cards.CalcCardFix(BattleAttackType.Weapon, attacker, dark, 1000, false));
        sc.End(attacker, StatusType.Basilica);
        Assert.Equal(1000, cards.CalcCardFix(BattleAttackType.Weapon, attacker, dark, 1000, false)); // no leak
    }

    // ---- magic_atk_ele[Holy] += val1*3 (read live in CalcMagicAttack) ----

    [Fact]
    public void Basilica_holy_magic_buff_adds_val1_times_3()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var caster = NewPlayer();
        caster.Stats.MatkMin = caster.Stats.MatkMax = 1000;
        caster.Stats.WeaponElement = (byte)BattleElement.Holy; // → resolved magic element Holy
        var target = MakeMob(BattleElement.Neutral);
        target.Stats.Mdef = 0; target.Stats.Mdef2 = 0;

        var calc = new BattleCalculator(rng: new Random(0), sc: sc);
        long without = calc.CalcMagicAttack(caster, target, SkillIds.AL_HOLYLIGHT, 1, 100).Damage;
        sc.Start(caster, StatusType.Basilica, val1: 5, 0, 0, 0, durationMs: 30_000, caster); // +15%
        long with = calc.CalcMagicAttack(caster, target, SkillIds.AL_HOLYLIGHT, 1, 100).Damage;

        Assert.Equal(without * (100 + 15) / 100, with);
    }

    // ---- NoAttack caster state ----

    [Fact]
    public void Basilica_blocks_auto_attack_but_allows_casting()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = NewPlayer();

        Assert.True(pc.CanAttack(sc));      // baseline
        Assert.True(pc.CanCastSkill(sc));

        sc.Start(pc, StatusType.Basilica, val1: 1, 0, 0, 0, durationMs: 30_000, pc);
        Assert.False(pc.CanAttack(sc));     // NoAttack
        Assert.True(pc.CanCastSkill(sc));   // …but can re-cast to cancel it
        Assert.True(pc.CanAct(sc));
    }

    // ---- helpers ----

    private static PlayerEntity NewPlayer()
    {
        var pc = new PlayerEntity(1, 1, "Priest", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Race = BattleRace.Demihuman; pc.Stats.Size = BattleSize.Medium;
        pc.Stats.DefenseElement = BattleElement.Holy; pc.Stats.ElementLevel = 1;
        pc.Stats.AttackRange = 1;
        return pc;
    }

    private static MobEntity MakeMob(BattleElement defEle)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Race = BattleRace.Formless; m.Stats.Size = BattleSize.Medium;
        m.Stats.DefenseElement = defEle; m.Stats.ElementLevel = 1;
        m.Stats.WeaponElement = (byte)BattleElement.Neutral;
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }
}
