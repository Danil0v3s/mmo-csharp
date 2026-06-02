using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-22 — bonus2 per-skill maps: bSkillAtk (per-skill % damage) +
/// the per-skill cast-rate maps (data for COMBAT-24). rAthena
/// pc_skillatk_bonus (battle.cpp:7729) / SP_SKILL_ATK (pc.cpp:4627).
/// </summary>
public class Combat22SkillAtkTests
{
    // ---- extractor: skill-name resolution (constant + quoted + numeric) ----

    [Fact]
    public void Extractor_parses_bSkillAtk_by_name()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSkillAtk,MG_FIREBOLT,20;", b);
        Assert.Equal(20, b.SkillAtk.GetValueOrDefault(SkillIds.MG_FIREBOLT));
    }

    [Fact]
    public void Extractor_parses_bSkillAtk_quoted_and_numeric()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSkillAtk,\"MG_COLDBOLT\",15;", b);
        Assert.Equal(15, b.SkillAtk.GetValueOrDefault(SkillIds.MG_COLDBOLT));

        var b2 = new EquipBonusBundle();
        BonusScriptExtractor.Apply($"bonus2 bSkillAtk,{SkillIds.MG_FIREBOLT},10;", b2);
        Assert.Equal(10, b2.SkillAtk.GetValueOrDefault(SkillIds.MG_FIREBOLT));
    }

    [Fact]
    public void Extractor_stores_per_skill_castrate_inversed()
    {
        // rAthena stores bonus2 bVariableCastrate as -val (a reduction).
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bVariableCastrate,WZ_METEOR,30; bonus2 bFixedCastrate,WZ_METEOR,10;", b);
        Assert.Equal(-30, b.SkillVarCastrate.GetValueOrDefault(SkillIds.WZ_METEOR));
        Assert.Equal(-10, b.SkillFixCastrate.GetValueOrDefault(SkillIds.WZ_METEOR));
    }

    [Fact]
    public void Extractor_skips_unknown_skill_name()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSkillAtk,NOT_A_SKILL,99;", b);
        Assert.Empty(b.SkillAtk);
    }

    // ---- magic consumer: per-skill, scoped ----

    [Fact]
    public void Magic_skillatk_applies_only_to_the_named_skill()
    {
        var calc = new BattleCalculator(new Random(0));
        var caster = NewMage(matk: 200);
        caster.EquipBonuses.SkillAtk[SkillIds.MG_FIREBOLT] = 20;
        var target = MakeMob();

        // Fire Bolt → +20%; Cold Bolt (not carded) → unchanged.
        Assert.Equal(240, calc.CalcMagicAttack(caster, target, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100).Damage);
        Assert.Equal(200, calc.CalcMagicAttack(caster, target, SkillIds.MG_COLDBOLT, 5, ratePerLevel: 100).Damage);
    }

    // ---- weapon consumer ----

    [Fact]
    public void Weapon_skillatk_applies_after_ratio()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.EquipBonuses.SkillAtk[SkillIds.SM_BASH] = 20;
        var swing = new BattleDamage { Damage = 100, Lane = BattleAttackType.Weapon };
        var plugin = new TestWeaponSkill(SkillIds.SM_BASH);

        // ratio 100 → 100, then +20% skillatk → 120.
        Assert.Equal(120, plugin.ComputeSkillDamage(swing, pc, MakeMob(), 1));
        // An attacker without the card gets the raw 100.
        var bare = new PlayerEntity(2, 2, "Bare", Guid.NewGuid(), 0, 0, 0);
        Assert.Equal(100, plugin.ComputeSkillDamage(swing, bare, MakeMob(), 1));
    }

    // ---- helpers ----

    private sealed class TestWeaponSkill : WeaponSkillImpl
    {
        public TestWeaponSkill(ushort id) : base(id) { }
    }

    private static PlayerEntity NewMage(int matk)
    {
        var p = new PlayerEntity(1, 1, "Mage", Guid.NewGuid(), 0, 0, 0);
        p.Stats.MatkMin = p.Stats.MatkMax = (ushort)matk;
        return p;
    }

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Mdef = 0; m.Stats.Mdef2 = 0; m.Stats.Def = 0; m.Stats.Def2 = 0;
        return m;
    }
}
