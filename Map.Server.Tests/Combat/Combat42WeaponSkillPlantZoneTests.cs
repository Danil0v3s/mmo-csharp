using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-42 — weapon SKILLS now run the post-ratio plant 1-damage clamp +
/// GvG/BG zone scaling + SC_INVINCIBLE via
/// <see cref="IBattleCalculator.ApplyWeaponSkillPlantZone"/> (rAthena
/// battle_calc_weapon_attack), which the skill dispatch calls before ApplyDamage.
/// </summary>
public class Combat42WeaponSkillPlantZoneTests
{
    [Fact]
    public void Melee_weapon_skill_on_an_ignore_melee_plant_deals_one()
    {
        var calc = new BattleCalculator();
        var plant = NewTarget();
        plant.Stats.Mode = MobMode.IgnoreMelee;   // Flora / Hydra

        Assert.Equal(1, calc.ApplyWeaponSkillPlantZone(NewPc(), plant, 5000, isShortRange: true));
    }

    [Fact]
    public void Ranged_plant_only_clamps_a_ranged_skill()
    {
        var calc = new BattleCalculator();
        var plant = NewTarget();
        plant.Stats.Mode = MobMode.IgnoreRanged;

        // A melee swing is unaffected by MD_IGNORERANGED; a ranged one is clamped.
        Assert.Equal(5000, calc.ApplyWeaponSkillPlantZone(NewPc(), plant, 5000, isShortRange: true));
        Assert.Equal(1, calc.ApplyWeaponSkillPlantZone(NewPc(), plant, 5000, isShortRange: false));
    }

    [Fact]
    public void Sc_invincible_target_takes_one_from_a_weapon_skill()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var calc = new BattleCalculator(sc: sc);
        var target = NewTarget();
        sc.Start(target, StatusType.Invincible, 0, 0, 0, 0, 60_000);

        Assert.Equal(1, calc.ApplyWeaponSkillPlantZone(NewPc(), target, 5000, isShortRange: true));
    }

    [Fact]
    public void Weapon_skill_routes_through_the_zone_scaler()
    {
        // Stub zone scales weapon-skill damage to 60% (the GvG weapon rate).
        var calc = new BattleCalculator(zone: new StubZone(60));
        Assert.Equal(3000, calc.ApplyWeaponSkillPlantZone(NewPc(), NewTarget(), 5000, isShortRange: true));
    }

    [Fact]
    public void Plant_takes_precedence_over_zone()
    {
        // rAthena: is_infinite_defense returns before gvg_bg — a plant on a GvG map
        // still deals 1, not 1*60%.
        var calc = new BattleCalculator(zone: new StubZone(60));
        var plant = NewTarget();
        plant.Stats.Mode = MobMode.IgnoreMelee;
        Assert.Equal(1, calc.ApplyWeaponSkillPlantZone(NewPc(), plant, 5000, isShortRange: true));
    }

    // ---- helpers ----

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);

    private static MobEntity NewTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "FLORA", Name = "Flora", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.MaxHp = m.Hp = 5000;
        return m;
    }

    private sealed class StubZone : IZoneDamageService
    {
        private readonly int _rate;
        public StubZone(int rate) => _rate = rate;
        public long Scale(BattleAttackType lane, Entity src, Entity target, long damage,
            bool isSkill, bool isShortRange, ushort skillId)
            => damage * _rate / 100;
    }
}
