using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Skills.Behaviors.Thief;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-38 — per-skill div_ switch arms. Each plugin encodes its weapon-type /
/// target-size div in <c>ModifyDamageData</c>; that hook was dead (never invoked)
/// and is now wired into the WeaponSkillImpl damage path. These assert the div the
/// hook produces (display count) — KN_PIERCE → size+1, KN_BOWLINGBASH 2HSword → 2,
/// SC_FATALMENACE dagger → base+1. (The splash/SkillImpl arms — RK_WINDCUTTER,
/// RG_BACKSTAP — and the positive-div damage multiply are COMBAT-60.)
/// </summary>
public class Combat38PerSkillDivTests
{
    [Theory]
    [InlineData(BattleSize.Small, 1)]
    [InlineData(BattleSize.Medium, 2)]
    [InlineData(BattleSize.Large, 3)]
    public void Pierce_div_is_target_size_plus_one(BattleSize size, int expectedDiv)
        => Assert.Equal(expectedDiv, DivOf(new Pierce(), size, weapon: WeaponTypeCodes.OneHandSpear));

    [Fact]
    public void BowlingBash_two_hand_sword_renders_two_hits()
        => Assert.Equal(2, DivOf(new BowlingBash(), BattleSize.Medium, WeaponTypeCodes.TwoHandSword));

    [Fact]
    public void BowlingBash_other_weapon_keeps_its_base_two_hits()
        // COMBAT-39: KN_BOWLINGBASH HitCount = 2 is the base for every weapon; the
        // 2HSword/miscflag tiers (3/4) refine it (COMBAT-60), so a non-2HSword still
        // renders the base 2.
        => Assert.Equal(2, DivOf(new BowlingBash(), BattleSize.Medium, WeaponTypeCodes.OneHandSword));

    [Fact]
    public void FatalMenace_with_a_dagger_adds_one_hit()
        => Assert.Equal(2, DivOf(new FatalMenace(), BattleSize.Medium, WeaponTypeCodes.Dagger));

    [Fact]
    public void FatalMenace_without_a_dagger_is_single_hit()
        => Assert.Equal(1, DivOf(new FatalMenace(), BattleSize.Medium, WeaponTypeCodes.OneHandSword));

    /// <summary>
    /// Invokes the plugin's <see cref="WeaponSkillImpl.ModifyDamageData"/> exactly as
    /// the wired damage path does — seeding the base display count then letting the
    /// per-skill arm adjust it — and returns the resulting display div.
    /// </summary>
    private static int DivOf(WeaponSkillImpl plugin, BattleSize size, int weapon)
    {
        var src = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = weapon };
        var target = MakeTarget(size);
        var bd = new BattleDamage { Damage = 1000, Hits = plugin.GetMultiHitCount(skillLevel: 5) };
        plugin.ModifyDamageData(ref bd, src, target, skillLevel: 5);
        return Math.Abs(bd.Hits);
    }

    private static MobEntity MakeTarget(BattleSize size)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Size = size;
        return m;
    }
}
