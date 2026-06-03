using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-78 — the crit_atk_rate ÷200 skill variant (battle.cpp:7787). On a critical SKILL the
/// caster's <c>bonus bCritAtkRate</c> bumps damage by <c>÷200</c> (vs the auto-attack <c>÷100</c>),
/// applied after the skill ratio. The skill swing is built crit_atk_rate-free
/// (<c>CalcWeaponAttack(skillId)</c>) so it is NOT double-counted.
///
/// Fixture: bare weapon roll pinned to 100, Cri 1000 (guaranteed crit vs Luk 0 with the zero roll).
/// A critical base hand = 100 * 14/10 = 140. Identity skill ratio (100). So:
///   skill, car 0  → 140
///   skill, car 50 → 140 + 140*50/200 = 175   (÷200)
///   normal, car 50 → 140 + 140*50/100 = 210  (÷100, COMBAT-61 — unchanged)
/// </summary>
public class Combat78SkillCritAtkRateTests
{
    /// <summary>Identity-ratio weapon skill (default CalculateSkillRatio = 100).</summary>
    private sealed class Ratio100Skill : WeaponSkillImpl
    {
        public Ratio100Skill() : base(SkillIds.SM_BASH) { }
    }

    [Fact]
    public void Critical_skill_applies_crit_atk_rate_over_200()
    {
        var pc = MakeSwinger(); pc.Stats.Cri = 1000;
        pc.EquipBonuses.CritAtkRate = 50;
        var target = MakeTarget();
        var calc = new BattleCalculator(rng: new ZeroRandom());

        var swing = calc.CalcWeaponAttack(pc, target, SkillIds.SM_BASH);
        Assert.True(swing.IsCritical);                 // the swing crit (Cri 1000)
        Assert.Equal(140, swing.Total);                // …but carries NO crit_atk_rate bump (÷200 deferred)

        var dmg = new Ratio100Skill().ComputeSkillDamage(swing, pc, target, 1, ctx: null);
        Assert.Equal(175, dmg);                        // 140 + 140*50/200
    }

    [Fact]
    public void Critical_skill_without_crit_atk_rate_is_unchanged()
    {
        var pc = MakeSwinger(); pc.Stats.Cri = 1000;   // car 0
        var calc = new BattleCalculator(rng: new ZeroRandom());
        var swing = calc.CalcWeaponAttack(pc, MakeTarget(), SkillIds.SM_BASH);
        Assert.Equal(140, new Ratio100Skill().ComputeSkillDamage(swing, pc, MakeTarget(), 1, ctx: null));
    }

    [Fact]
    public void Non_critical_skill_gets_no_crit_atk_rate()
    {
        var pc = MakeSwinger(); pc.Stats.Cri = 0;      // never crits
        pc.EquipBonuses.CritAtkRate = 50;
        var calc = new BattleCalculator(rng: new ZeroRandom());
        var swing = calc.CalcWeaponAttack(pc, MakeTarget(), SkillIds.SM_BASH);
        Assert.False(swing.IsCritical);
        Assert.Equal(100, new Ratio100Skill().ComputeSkillDamage(swing, pc, MakeTarget(), 1, ctx: null));
    }

    [Fact]
    public void Normal_attack_keeps_the_over_100_divisor()
    {
        // Regression guard: the auto-attack (skill_id 0) crit_atk_rate stays ÷100 (COMBAT-61).
        var pc = MakeSwinger(); pc.Stats.Cri = 1000;
        pc.EquipBonuses.CritAtkRate = 50;
        var calc = new BattleCalculator(rng: new ZeroRandom());
        Assert.Equal(210, calc.CalcWeaponAttack(pc, MakeTarget()).Damage); // 140 + 140*50/100
    }

    // ---- helpers (mirror COMBAT-61) ----

    private static PlayerEntity MakeSwinger()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = 0 };
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100;
        pc.Stats.Dex = 100;
        pc.Stats.Batk = 0; pc.Stats.Cri = 0; pc.Stats.Hit = 10000;
        pc.Stats.Patk = 0;
        pc.Stats.WeaponElement = 0;
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Res = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
