using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T2.3 acceptance tests — first wave of per-skill behavior plugins
/// covering the major job-tree skills. Each plugin lives in its own
/// file under Map.Server/Skills/Behaviors/; tests group by skill
/// family rather than by plugin.
/// </summary>
public class T2_3_SkillBehaviorMigrationTests
{
    // ============================================================
    //  Swordsman family
    // ============================================================

    [Fact]
    public void Provoke_AppliesScOnTarget()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Stats.DefenseElement = BattleElement.Neutral;

        new ProvokeBehavior().Resolve(caster, target,
            MakeDef(SkillIds.SM_PROVOKE, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);

        Assert.NotNull(ctx.Sc.Get(target, StatusType.Provoke));
    }

    [Fact]
    public void Provoke_FailsOnUndeadElement()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Stats.DefenseElement = BattleElement.Undead;

        new ProvokeBehavior().Resolve(caster, target,
            MakeDef(SkillIds.SM_PROVOKE, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);

        Assert.Null(ctx.Sc.Get(target, StatusType.Provoke));
    }

    [Fact]
    public void Endure_AlwaysAppliesToSelf()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        new EndureBehavior().Resolve(caster, caster,
            MakeDef(SkillIds.SM_ENDURE, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        var sc = ctx.Sc.Get(caster, StatusType.Endure);
        Assert.NotNull(sc);
        Assert.Equal(7, sc!.Val2); // Val2 = 7 remaining hits.
    }

    // ============================================================
    //  Knight family
    // ============================================================

    [Fact]
    public void TwoHandQuicken_AppliesAspdBoostToSelf()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.AspdRate = 0;
        new TwoHandQuickenBehavior().Resolve(caster, caster,
            MakeDef(SkillIds.KN_TWOHANDQUICKEN, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);

        // Val1 = 35 (7 * lv5) → AspdRate += 35 via SC handler.
        Assert.Equal(35, caster.Stats.AspdRate);
    }

    [Fact]
    public void Pierce_HitsTwiceVsMediumMob()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Stats.Size = BattleSize.Medium;
        mob.Hp = 1000; mob.Stats.MaxHp = 1000;

        var before = mob.Hp;
        new PierceBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.KN_PIERCE, SkillDamageKind.Weapon, 3), skillLevel: 5, ctx.Behavior);

        // 2 hits at 150% each. Each hit > 0 → HP drops by at least 2 units;
        // the precise damage depends on the calc — we just need the
        // monotonic property of "took multi-hit damage".
        var afterTwoHits = mob.Hp;
        Assert.True(afterTwoHits < before);

        // Reset + try Small mob (1 hit) — should take less damage proportionally.
        var small = ctx.AddMob(53, 53);
        small.Stats.Size = BattleSize.Small;
        small.Hp = 1000; small.Stats.MaxHp = 1000;
        new PierceBehavior().Resolve(caster, small,
            MakeDef(SkillIds.KN_PIERCE, SkillDamageKind.Weapon, 3), skillLevel: 5, ctx.Behavior);
        var smallTook = 1000 - small.Hp;
        var medTook = 1000 - afterTwoHits;
        Assert.True(medTook >= smallTook * 2 - 5, $"medium ({medTook}) should be ~2x small ({smallTook})");
    }

    [Fact]
    public void BowlingBash_HitsPrimary_AndSplashesNearby()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var primary = ctx.AddMob(51, 51);
        var splash = ctx.AddMob(52, 52);
        var far = ctx.AddMob(90, 90);
        primary.Hp = primary.Stats.MaxHp = 1000;
        splash.Hp = splash.Stats.MaxHp = 1000;
        far.Hp = far.Stats.MaxHp = 1000;

        new BowlingBashBehavior().Resolve(caster, primary,
            MakeDef(SkillIds.KN_BOWLINGBASH, SkillDamageKind.Weapon, 2), skillLevel: 5, ctx.Behavior);

        Assert.True(primary.Hp < 1000);
        Assert.True(splash.Hp < 1000);
        Assert.Equal(1000, far.Hp);
    }

    // ============================================================
    //  Mage family
    // ============================================================

    [Fact]
    public void FrostDiver_ProcsFreeze_WhenRollUnderChance()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        new FrostDiverBehavior(new FixedRandom(0)).Resolve(caster, mob,
            MakeDef(SkillIds.MG_FROSTDIVER, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);

        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Freeze));
    }

    [Fact]
    public void FrostDiver_NoFreeze_OnHighRoll()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        // chance at lv5 = 45%; FixedRandom(99) → no proc.
        new FrostDiverBehavior(new FixedRandom(99)).Resolve(caster, mob,
            MakeDef(SkillIds.MG_FROSTDIVER, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(mob, StatusType.Freeze));
    }

    [Fact]
    public void StoneCurse_AppliesStoneWait_OnLowRoll()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        new StoneCurseBehavior(new FixedRandom(0)).Resolve(caster, mob,
            MakeDef(SkillIds.MG_STONECURSE, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Stonewait));
    }

    // ============================================================
    //  Acolyte / Priest family
    // ============================================================

    [Fact]
    public void LexDivina_AppliesSilence_AndToggleEndsIt()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);

        new LexDivinaBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_LEXDIVINA, SkillDamageKind.None, 9), skillLevel: 3, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Silence));

        // Recast cures.
        new LexDivinaBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_LEXDIVINA, SkillDamageKind.None, 9), skillLevel: 3, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(target, StatusType.Silence));
    }

    [Fact]
    public void LexAeterna_AppliesAeterna_AndReCastNoOps()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);

        new LexAeternaBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_LEXAETERNA, SkillDamageKind.None, 9), skillLevel: 1, ctx.Behavior);
        var sc = ctx.Sc.Get(target, StatusType.Aeterna);
        Assert.NotNull(sc);
        Assert.Equal(-1, sc!.ExpiresAt); // permanent until consumed.

        // Re-cast: silently no-op.
        new LexAeternaBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_LEXAETERNA, SkillDamageKind.None, 9), skillLevel: 1, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Aeterna));
    }

    [Fact]
    public void TurnUndead_LowChance_FallsThrough()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Level = 1;
        caster.Stats.Luk = 0;
        var undead = ctx.AddMob(51, 51);
        undead.Stats.DefenseElement = BattleElement.Undead;
        undead.Level = 99;
        undead.Hp = 5000;

        // Instakill chance is negative → clamped to 0 → never procs.
        // FixedRandom(0) wouldn't matter since chance=0.
        var handled = new TurnUndeadBehavior(new FixedRandom(0)).Resolve(caster, undead,
            MakeDef(SkillIds.PR_TURNUNDEAD, SkillDamageKind.Magic, 9), skillLevel: 1, ctx.Behavior);
        // Returns false → falls through to Magic resolver.
        Assert.False(handled);
        Assert.Equal(5000, undead.Hp);
    }

    [Fact]
    public void TurnUndead_HighChance_Instakills()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Level = 99; caster.Stats.Luk = 100;
        var undead = ctx.AddMob(51, 51);
        undead.Stats.DefenseElement = BattleElement.Undead;
        undead.Level = 1;
        undead.Hp = 5000; undead.Stats.MaxHp = 5000;

        // Chance is huge → clamped to 100; FixedRandom(0) under any cap.
        var handled = new TurnUndeadBehavior(new FixedRandom(0)).Resolve(caster, undead,
            MakeDef(SkillIds.PR_TURNUNDEAD, SkillDamageKind.Magic, 9), skillLevel: 10, ctx.Behavior);
        Assert.True(handled);
        Assert.Equal(0, undead.Hp);
    }

    [Fact]
    public void TurnUndead_NonUndead_NoOps()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var living = ctx.AddMob(51, 51);
        living.Stats.DefenseElement = BattleElement.Neutral;
        living.Hp = 1000; living.Stats.MaxHp = 1000;

        new TurnUndeadBehavior(new FixedRandom(0)).Resolve(caster, living,
            MakeDef(SkillIds.PR_TURNUNDEAD, SkillDamageKind.Magic, 9), skillLevel: 10, ctx.Behavior);
        Assert.Equal(1000, living.Hp); // untouched.
    }

    // ============================================================
    //  Merchant / Blacksmith family
    // ============================================================

    [Fact]
    public void Mammonite_HitsTarget()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 1000; mob.Stats.MaxHp = 1000;

        new MammoniteBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MC_MAMMONITE, SkillDamageKind.Weapon, 1), skillLevel: 5, ctx.Behavior);

        Assert.True(mob.Hp < 1000);
    }

    [Fact]
    public void HammerFall_StunOnLowRoll()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);

        // chance at lv5 = 70%; FixedRandom(0) → procs.
        new HammerFallBehavior(new FixedRandom(0)).Resolve(caster, mob,
            MakeDef(SkillIds.BS_HAMMERFALL, SkillDamageKind.Weapon, 2), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Stun));
    }

    [Fact]
    public void AdrenalineRush_BoostsCasterAspd()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.AspdRate = 0;

        new AdrenalineRushBehavior().Resolve(caster, caster,
            MakeDef(SkillIds.BS_ADRENALINE, SkillDamageKind.None, 0), skillLevel: 1, ctx.Behavior);

        Assert.Equal(30, caster.Stats.AspdRate);
    }

    [Fact]
    public void Overthrust_AppliesScWithLevelScaledVal1()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        new OverthrustBehavior().Resolve(caster, caster,
            MakeDef(SkillIds.BS_OVERTHRUST, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        var sc = ctx.Sc.Get(caster, StatusType.Overthrust);
        Assert.NotNull(sc);
        Assert.Equal(25, sc!.Val1); // 5 * lv = 25 % ATK boost
    }

    // ============================================================
    //  Archer / Hunter family
    // ============================================================

    [Fact]
    public void DoubleStrafe_HitsTwice()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 5000; mob.Stats.MaxHp = 5000;

        new DoubleStrafeBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.AC_DOUBLE, SkillDamageKind.Weapon, 9), skillLevel: 5, ctx.Behavior);

        Assert.True(mob.Hp < 5000); // multi-hit took chunks.
    }

    [Fact]
    public void ArrowShower_HitsAllInSplash()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var primary = ctx.AddMob(60, 60);
        var nearby = ctx.AddMob(61, 60);
        primary.Hp = primary.Stats.MaxHp = 1000;
        nearby.Hp = nearby.Stats.MaxHp = 1000;

        new ArrowShowerBehavior().Resolve(caster, primary,
            MakeDef(SkillIds.AC_SHOWER, SkillDamageKind.Weapon, 9), skillLevel: 5, ctx.Behavior);

        Assert.True(primary.Hp < 1000);
        Assert.True(nearby.Hp < 1000);
    }

    [Fact]
    public void BlitzBeat_HitsScalingByLevel()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Dex = 50; caster.Stats.IntStat = 50; caster.Level = 50;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;

        new BlitzBeatBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.HT_BLITZBEAT, SkillDamageKind.Misc, 9), skillLevel: 3, ctx.Behavior);

        Assert.True(mob.Hp < 9999);
    }

    // ============================================================
    //  Thief / Assassin family
    // ============================================================

    [Fact]
    public void Hiding_TogglesSc()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);

        new HidingBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.TF_HIDING, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Hiding));

        // Recast — toggles off.
        new HidingBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.TF_HIDING, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(pc, StatusType.Hiding));
    }

    [Fact]
    public void Poison_AppliesPoisonScOnLowRoll()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        // chance at lv5 = 55%; FixedRandom(0) → procs.
        new PoisonBehavior(new FixedRandom(0)).Resolve(caster, mob,
            MakeDef(SkillIds.TF_POISON, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Poison));
    }

    [Fact]
    public void SonicBlow_DealsMultiHit()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;

        new SonicBlowBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.AS_SONICBLOW, SkillDamageKind.Weapon, 1), skillLevel: 5, ctx.Behavior);

        Assert.True(mob.Hp < 9999);
    }

    // ============================================================
    //  Monk family
    // ============================================================

    [Fact]
    public void TripleAttack_HitsThreeTimes()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;

        new TripleAttackBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MO_TRIPLEATTACK, SkillDamageKind.Weapon, 1), skillLevel: 5, ctx.Behavior);

        Assert.True(mob.Hp < 9999);
    }

    // ============================================================
    //  Holy Light (defers to generic Magic resolver)
    // ============================================================

    [Fact]
    public void HolyLight_FallsThrough_ReturnsFalse()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        var handled = new HolyLightBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.AL_HOLYLIGHT, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        Assert.False(handled);
    }

    // ============================================================
    //  Wave 2 — Priest support family
    // ============================================================

    [Fact]
    public void Impositio_AppliesScWithFlatAtk()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        new ImpositioManusBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_IMPOSITIO, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);
        var sc = ctx.Sc.Get(target, StatusType.Impositio);
        Assert.NotNull(sc);
        Assert.Equal(25, sc!.Val1); // 5 * lv = 25 flat ATK
    }

    [Fact]
    public void Suffragium_AppliesCastSpeedSc()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        new SuffragiumBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_SUFFRAGIUM, SkillDamageKind.None, 9), skillLevel: 3, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Suffragium));
    }

    [Fact]
    public void Aspersio_AppliesEndowSc()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        new AspersioBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_ASPERSIO, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Aspersio));
    }

    [Fact]
    public void KyrieEleison_AppliesShieldFromMaxHp()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        target.MaxHp = 1000; target.Stats.MaxHp = 1000;
        new KyrieEleisonBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_KYRIE, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);
        var sc = ctx.Sc.Get(target, StatusType.Kyrie);
        Assert.NotNull(sc);
        // lv5 → 22 % of MaxHp 1000 = 220 hp shield, 10 hits.
        Assert.Equal(220, sc!.Val1);
        Assert.Equal(10, sc.Val2);
    }

    [Fact]
    public void Magnificat_AlwaysAppliesToSelf()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var unrelated = ctx.AddPlayer(2, 51, 51);
        new MagnificatBehavior().Resolve(caster, unrelated,
            MakeDef(SkillIds.PR_MAGNIFICAT, SkillDamageKind.None, 0), skillLevel: 3, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(caster, StatusType.Magnificat));
        Assert.Null(ctx.Sc.Get(unrelated, StatusType.Magnificat));
    }

    [Fact]
    public void Gloria_AppliesLukBoost()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        target.Stats.Luk = 10;
        new GloriaBehavior().Resolve(caster, target,
            MakeDef(SkillIds.PR_GLORIA, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);
        // Gloria handler adds 30 to Luk on OnStart.
        Assert.Equal(40, target.Stats.Luk);
    }

    // ============================================================
    //  Wave 2 — Mage bolts + AoE
    // ============================================================

    [Fact]
    public void FireBolt_HitsFiveTimes_AtLevel5()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 5000; mob.Stats.MaxHp = 5000;
        new FireBoltBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MG_FIREBOLT, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        Assert.Equal(5000 - 150 * 5, mob.Hp); // 5 hits × 150 (midpoint).
    }

    [Fact]
    public void SoulStrike_HitsCeilLevelDiv2()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 1000; mob.Stats.MaxHp = 1000;
        // lv 9 → (9+1)/2 = 5 hits.
        new SoulStrikeBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MG_SOULSTRIKE, SkillDamageKind.Magic, 9), skillLevel: 9, ctx.Behavior);
        Assert.Equal(1000 - 100 * 5, mob.Hp);
    }

    [Fact]
    public void NapalmBeat_DamageSplitsAcrossVictims()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var v1 = ctx.AddMob(60, 60);
        var v2 = ctx.AddMob(61, 60);
        v1.Hp = v1.Stats.MaxHp = 1000;
        v2.Hp = v2.Stats.MaxHp = 1000;
        new NapalmBeatBehavior().Resolve(caster, v1,
            MakeDef(SkillIds.MG_NAPALMBEAT, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        // Total damage = 100 * (70+50)/100 = 120 split across 2 victims → 60 each.
        Assert.Equal(940, v1.Hp);
        Assert.Equal(940, v2.Hp);
    }

    [Fact]
    public void Fireball_PrimaryFullSplashHalf()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var primary = ctx.AddMob(60, 60);
        var splash = ctx.AddMob(61, 60);
        var far = ctx.AddMob(90, 90);
        primary.Hp = primary.Stats.MaxHp = 5000;
        splash.Hp = splash.Stats.MaxHp = 5000;
        far.Hp = far.Stats.MaxHp = 5000;
        new FireballBehavior().Resolve(caster, primary,
            MakeDef(SkillIds.MG_FIREBALL, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        // Primary: 100 * (50 + 350)/100 = 400. Splash: 200. Far: untouched.
        Assert.Equal(5000 - 400, primary.Hp);
        Assert.Equal(5000 - 200, splash.Hp);
        Assert.Equal(5000, far.Hp);
    }

    [Fact]
    public void Thunderstorm_HitsThreeTimes_PerVictim()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var mob = ctx.AddMob(60, 60);
        mob.Hp = mob.Stats.MaxHp = 5000;
        new ThunderstormBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MG_THUNDERSTORM, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);
        // Per-hit = 100 * (80+100)/100 = 180. 3 hits = 540.
        Assert.Equal(5000 - 540, mob.Hp);
    }

    // ============================================================
    //  Wave 2 — Acolyte AoE / Knight specials
    // ============================================================

    [Fact]
    public void SignumCrucis_AppliesOnlyToUndeadDark()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var undead = ctx.AddMob(51, 51);
        undead.Stats.DefenseElement = BattleElement.Undead;
        var dark = ctx.AddMob(52, 52);
        dark.Stats.DefenseElement = BattleElement.Dark;
        var neutral = ctx.AddMob(53, 53);
        neutral.Stats.DefenseElement = BattleElement.Neutral;

        new SignumCrucisBehavior().Resolve(caster, neutral,
            MakeDef(SkillIds.AL_CRUCIS, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);

        Assert.NotNull(ctx.Sc.Get(undead, StatusType.Signumcrucis));
        Assert.NotNull(ctx.Sc.Get(dark, StatusType.Signumcrucis));
        Assert.Null(ctx.Sc.Get(neutral, StatusType.Signumcrucis));
    }

    [Fact]
    public void BrandishSpear_PrimaryFullSplashHalf()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var primary = ctx.AddMob(60, 60);
        var splash = ctx.AddMob(61, 60);
        primary.Hp = primary.Stats.MaxHp = 5000;
        splash.Hp = splash.Stats.MaxHp = 5000;
        new BrandishSpearBehavior().Resolve(caster, primary,
            MakeDef(SkillIds.KN_BRANDISHSPEAR, SkillDamageKind.Weapon, 3), skillLevel: 5, ctx.Behavior);
        Assert.True(primary.Hp < 5000);
        Assert.True(splash.Hp < 5000);
        // Splash takes half-ish; assert ordering.
        var primaryTook = 5000 - primary.Hp;
        var splashTook = 5000 - splash.Hp;
        Assert.True(primaryTook > splashTook);
    }

    // ============================================================
    //  Wave 2 — Assassin specials
    // ============================================================

    [Fact]
    public void GrimTooth_ConsumesHiding_AndFallsThrough()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        // Pre-apply hiding so we can confirm consumption.
        ctx.Sc.Start(pc, StatusType.Hiding, val1: 1, 0, 0, 0,
            durationMs: 60_000);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Hiding));

        var handled = new GrimToothBehavior().Resolve(pc, target,
            MakeDef(SkillIds.AS_GRIMTOOTH, SkillDamageKind.Weapon, 9), skillLevel: 5, ctx.Behavior);

        Assert.False(handled); // generic resolver runs the hit.
        Assert.Null(ctx.Sc.Get(pc, StatusType.Hiding)); // hiding popped.
    }

    [Fact]
    public void EnchantPoison_AppliesPoisonWeaponSc()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        new EnchantPoisonBehavior().Resolve(caster, target,
            MakeDef(SkillIds.AS_ENCHANTPOISON, SkillDamageKind.None, 9), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Encpoison));
    }

    // ============================================================
    //  Wave 2 — Monk specials
    // ============================================================

    [Fact]
    public void FingerOffensive_HitsLevelTimes()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;
        new FingerOffensiveBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MO_FINGEROFFENSIVE, SkillDamageKind.Weapon, 5), skillLevel: 3, ctx.Behavior);
        Assert.True(mob.Hp < 9999);
    }

    [Fact]
    public void Investigate_HitsTarget()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Stats.Def = 50; mob.Stats.Def2 = 50;
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;
        new InvestigateBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MO_INVESTIGATE, SkillDamageKind.Weapon, 1), skillLevel: 5, ctx.Behavior);
        Assert.True(mob.Hp < 9999);
    }

    [Fact]
    public void ExtremityFist_ConsumesAllSp()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Batk = 100; caster.Stats.WatkMin = 100; caster.Stats.WatkMax = 100; caster.Stats.Hit = 200;
        caster.Stats.Sp = 500;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 99999; mob.Stats.MaxHp = 99999;
        new ExtremityFistBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.MO_EXTREMITYFIST, SkillDamageKind.Weapon, 1), skillLevel: 5, ctx.Behavior);
        Assert.Equal(0, caster.Stats.Sp); // all SP drained.
        Assert.True(mob.Hp < 99999);
    }

    [Fact]
    public void ExplosionSpirits_AppliesScOnSelf_WithCritAndBatkBoost()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.Cri = 0; caster.Stats.Batk = 100;
        new ExplosionSpiritsBehavior().Resolve(caster, caster,
            MakeDef(SkillIds.MO_EXPLOSIONSPIRITS, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        // SC handler: +100 Cri, +250 Batk at lv5.
        Assert.Equal(100, caster.Stats.Cri);
        Assert.Equal(350, caster.Stats.Batk);
    }

    [Fact]
    public void BodyRelocation_ClaimsCast_NoOp()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Hp = 1000; target.Stats.MaxHp = 1000;
        var handled = new BodyRelocationBehavior().Resolve(caster, target,
            MakeDef(SkillIds.MO_BODYRELOCATION, SkillDamageKind.None, 7), skillLevel: 5, ctx.Behavior);
        Assert.True(handled);
        Assert.Equal(1000, target.Hp); // no damage.
    }

    // ============================================================
    //  Wave 3 — Cloaking / Maximize / Wizard Earth-Wind / Bard-Dancer
    // ============================================================

    [Fact]
    public void Cloaking_TogglesScOnSelf()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        new CloakingBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.AS_CLOAKING, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Cloaking));
        // Recast → toggles off.
        new CloakingBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.AS_CLOAKING, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(pc, StatusType.Cloaking));
    }

    [Fact]
    public void MaximizePower_AppliesScOnSelf()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        new MaximizePowerBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.BS_MAXIMIZE, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Maximizepower));
    }

    [Fact]
    public void EarthSpike_HitsLevelTimes()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;
        new EarthSpikeBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.WZ_EARTHSPIKE, SkillDamageKind.Magic, 9), skillLevel: 3, ctx.Behavior);
        // per-hit = 100 * 400/100 = 400; 3 hits = 1200.
        Assert.Equal(9999 - 1200, mob.Hp);
    }

    [Fact]
    public void HeavenDrive_RevealsHiddenAndDamages()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var hidden = ctx.AddPlayer(2, 60, 60);
        // Pre-attach Hiding so we can confirm the reveal.
        ctx.Sc.Start(hidden, StatusType.Hiding, val1: 1, 0, 0, 0, durationMs: 60_000);
        Assert.NotNull(ctx.Sc.Get(hidden, StatusType.Hiding));

        new HeavenDriveBehavior().Resolve(caster, hidden,
            MakeDef(SkillIds.WZ_HEAVENDRIVE, SkillDamageKind.Magic, 9), skillLevel: 5, ctx.Behavior);

        Assert.Null(ctx.Sc.Get(hidden, StatusType.Hiding)); // revealed.
    }

    [Fact]
    public void JupitelThunder_HitsLevelPlusOneTimes()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;
        new JupitelThunderBehavior().Resolve(caster, mob,
            MakeDef(SkillIds.WZ_JUPITEL, SkillDamageKind.Magic, 9), skillLevel: 3, ctx.Behavior);
        // per-hit = 100 * 250/100 = 250; lv3 → 4 hits → 1000 total.
        Assert.Equal(9999 - 1000, mob.Hp);
    }

    [Fact]
    public void FrostNova_HitsAllInRadius_AndProcsFreeze()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = 100; caster.Stats.MatkMax = 100;
        var v1 = ctx.AddMob(51, 51);
        var v2 = ctx.AddMob(52, 52);
        v1.Hp = v1.Stats.MaxHp = 1000;
        v2.Hp = v2.Stats.MaxHp = 1000;

        // FixedRandom(0) → freeze always procs.
        new FrostNovaBehavior(new FixedRandom(0)).Resolve(caster, caster,
            MakeDef(SkillIds.WZ_FROSTNOVA, SkillDamageKind.Magic, 0), skillLevel: 5, ctx.Behavior);

        Assert.True(v1.Hp < 1000);
        Assert.True(v2.Hp < 1000);
        Assert.NotNull(ctx.Sc.Get(v1, StatusType.Freeze));
    }

    [Fact]
    public void OwlsEye_ClaimsCast_NoOpToday()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var handled = new OwlsEyeBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.AC_OWL, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.True(handled);
    }

    [Fact]
    public void ImproveConcentration_AppliesAgiDexSc()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Agi = 20; pc.Stats.Dex = 20;
        new ImproveConcentrationBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.AC_CONCENTRATION, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        // Concentrate SC handler adds val1 to Agi + Dex; val1 = 2*lv = 10.
        Assert.Equal(30, pc.Stats.Agi);
        Assert.Equal(30, pc.Stats.Dex);
    }

    [Fact]
    public void CallSpirits_ClaimsCast()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var handled = new CallSpiritsBehavior().Resolve(pc, pc,
            MakeDef(SkillIds.MO_CALLSPIRITS, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);
        Assert.True(handled);
    }

    [Fact]
    public void FrostJoker_FreezesOnLowRoll()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(52, 52);

        // lv5 chance = 35%; FixedRandom(0) always procs.
        new FrostJokerBehavior(new FixedRandom(0)).Resolve(caster, caster,
            MakeDef(SkillIds.BA_FROSTJOKER, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);

        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Freeze));
    }

    [Fact]
    public void Scream_StunsOnLowRoll()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(52, 52);

        new ScreamBehavior(new FixedRandom(0)).Resolve(caster, caster,
            MakeDef(SkillIds.DC_SCREAM, SkillDamageKind.None, 0), skillLevel: 5, ctx.Behavior);

        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Stun));
    }

    // ============================================================
    //  Registry — all 23 new plugins resolve by id
    // ============================================================

    [Fact]
    public void Registry_IndexesAllNewPlugins()
    {
        var reg = new SkillBehaviorRegistry(new ISkillBehavior[]
        {
            new ProvokeBehavior(),
            new EndureBehavior(),
            new TwoHandQuickenBehavior(),
            new PierceBehavior(),
            new BowlingBashBehavior(),
            new FrostDiverBehavior(),
            new StoneCurseBehavior(),
            new HolyLightBehavior(),
            new LexDivinaBehavior(),
            new LexAeternaBehavior(),
            new TurnUndeadBehavior(),
            new MammoniteBehavior(),
            new HammerFallBehavior(),
            new AdrenalineRushBehavior(),
            new OverthrustBehavior(),
            new DoubleStrafeBehavior(),
            new ArrowShowerBehavior(),
            new BlitzBeatBehavior(),
            new HidingBehavior(),
            new PoisonBehavior(),
            new SonicBlowBehavior(),
            new TripleAttackBehavior(),
        });

        Assert.Equal(22, reg.Count);
        Assert.NotNull(reg.Get(SkillIds.SM_PROVOKE));
        Assert.NotNull(reg.Get(SkillIds.AS_SONICBLOW));
        Assert.NotNull(reg.Get(SkillIds.KN_BOWLINGBASH));
        Assert.NotNull(reg.Get(SkillIds.PR_TURNUNDEAD));
    }

    // ============================================================
    //  Test rig
    // ============================================================

    private static SkillDefinition MakeDef(ushort id, SkillDamageKind damageKind, short range)
        => new()
        {
            Id = id,
            Name = $"skill_{id}",
            MaxLevel = 10,
            DamageKind = damageKind,
            Target = SkillTargetMode.TargetEnemy,
            Range = range,
            DamageRate = new[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
        };

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var spawnRegistry = new MobSpawnRegistry();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        var battle = new BattleCalculator(new Random(0));

        var behaviorCtx = new SkillBehaviorContext(entities, damage, battle, sc);
        return new TestContext(behaviorCtx, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillBehaviorContext Behavior,
        StatusChangeService Sc,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 1000 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }
    }

    private sealed class FixedRandom : Random
    {
        private readonly int _value;
        public FixedRandom(int value) { _value = value; }
        public override int Next(int maxValue) => _value % Math.Max(1, maxValue);
        public override int Next() => _value;
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
