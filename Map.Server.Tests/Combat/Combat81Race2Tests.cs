using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-81 — race2 (RaceGroups) cardfix folds: bAddRace2 / bSubRace2 / bMagicAddRace2, keyed on
/// the mob's race2 set (rAthena status_get_race2 / battle_calc_cardfix add_race2/sub_race2).
/// </summary>
public class Combat81Race2Tests
{
    private static BattleCardService Cards() => new(NullLogger<BattleCardService>.Instance);

    // ---- classifier ----

    [Fact]
    public void Classifier_maps_group_keys_and_rc2_tokens_to_the_same_id()
    {
        Assert.Equal(BattleRace2.Goblin, Race2Map.FromGroupKey("Goblin"));
        Assert.Equal(BattleRace2.Goblin, Race2Map.FromToken("RC2_GOBLIN"));
        Assert.Equal(BattleRace2.OghAtkDef, Race2Map.FromGroupKey("OghAtkDef"));
        Assert.Equal(BattleRace2.OghAtkDef, Race2Map.FromToken("RC2_OGH_ATK_DEF")); // snake vs Pascal
        Assert.Equal(BattleRace2.None, Race2Map.FromToken("RC2_NOPE"));
    }

    [Fact]
    public void FromRaceGroups_collects_the_enabled_groups()
    {
        var set = Race2Map.FromRaceGroups(new Dictionary<string, bool>
        {
            ["Goblin"] = true, ["Gvg"] = true, ["Golem"] = false, // disabled → excluded
        });
        Assert.Contains(BattleRace2.Goblin, set);
        Assert.Contains(BattleRace2.Gvg, set);
        Assert.DoesNotContain(BattleRace2.Golem, set);
    }

    // ---- offensive bAddRace2 ----

    [Fact]
    public void AddRace2_raises_weapon_damage_vs_the_targets_race2()
    {
        var src = NewPlayer();
        src.EquipBonuses.AddRace2[(int)BattleRace2.Goblin] = 20;
        var goblin = MakeMob("Goblin");
        Assert.Equal(1200, Cards().CalcCardFix(BattleAttackType.Weapon, src, goblin, 1000, leftHand: false));
        // a non-goblin mob is unaffected.
        Assert.Equal(1000, Cards().CalcCardFix(BattleAttackType.Weapon, src, MakeMob("Orc"), 1000, leftHand: false));
    }

    [Fact]
    public void AddRace2_sums_across_a_mobs_multiple_groups()
    {
        var src = NewPlayer();
        src.EquipBonuses.AddRace2[(int)BattleRace2.Goblin] = 20;
        src.EquipBonuses.AddRace2[(int)BattleRace2.Gvg] = 10;
        var both = MakeMob("Goblin", "Gvg");
        // summed 30% → one ×1.30 multiply (rAthena ranged/magic sum semantics).
        Assert.Equal(1300, Cards().CalcCardFix(BattleAttackType.Weapon, src, both, 1000, leftHand: false));
    }

    // ---- defensive bSubRace2 ----

    [Fact]
    public void SubRace2_reduces_incoming_damage_from_the_attackers_race2()
    {
        var attacker = MakeMob("Goblin");
        attacker.Stats.WeaponElement = (byte)BattleElement.Neutral;
        var target = NewPlayer();
        target.EquipBonuses.SubRace2[(int)BattleRace2.Goblin] = 20;
        Assert.Equal(800, Cards().CalcCardFix(BattleAttackType.Weapon, attacker, target, 1000, leftHand: false));
    }

    // ---- magic bMagicAddRace2 ----

    [Fact]
    public void MagicAddRace2_raises_magic_damage_vs_the_targets_race2()
    {
        var src = NewPlayer();
        src.EquipBonuses.MagicAddRace2[(int)BattleRace2.Goblin] = 20;
        var goblin = MakeMob("Goblin");
        Assert.Equal(1200, Cards().CalcCardFix(BattleAttackType.Magic, src, goblin, 1000, leftHand: false,
            attackElement: BattleElement.Neutral));
        // the WEAPON AddRace2 array does not leak into the magic lane.
        var src2 = NewPlayer();
        src2.EquipBonuses.AddRace2[(int)BattleRace2.Goblin] = 20;
        Assert.Equal(1000, Cards().CalcCardFix(BattleAttackType.Magic, src2, goblin, 1000, leftHand: false,
            attackElement: BattleElement.Neutral));
    }

    // ---- helpers ----

    private static PlayerEntity NewPlayer()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Race = BattleRace.Demihuman; pc.Stats.Size = BattleSize.Medium;
        pc.Stats.DefenseElement = BattleElement.Neutral; pc.Stats.ElementLevel = 1;
        return pc;
    }

    private static MobEntity MakeMob(params string[] raceGroups)
    {
        var db = new MobDbEntry
        {
            Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000,
            RaceGroups = raceGroups.ToDictionary(g => g, _ => true, StringComparer.OrdinalIgnoreCase),
        };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Race = BattleRace.Brute; m.Stats.Size = BattleSize.Medium;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }
}
