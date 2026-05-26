using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// NS-3 wave 1 acceptance tests — covers the 10 SCs whose
/// <see cref="StatusEffectHandler.OnStart"/> / <c>OnEnd</c> bodies were
/// promoted from <c>NoOpHandler()</c> placeholders to real
/// stat-field mutations in
/// <see cref="StatusEffectRegistry"/>:
///
/// Blind, Curse, WindWalk, Berserk, LaudaAgnus, LaudaRamus, Impositio,
/// Adoramus, DragonicAura, CartBoost.
///
/// Each test invokes <c>OnStart</c>, asserts the stat field changed by
/// the rAthena-formula amount, calls <c>OnEnd</c>, and asserts the
/// stat reverted to baseline. The registry is constructed standalone
/// so the tests don't depend on the rest of the SC-engine plumbing.
/// </summary>
public class StatusEffectNS3Wave1Tests
{
    private static readonly StatusEffectRegistry _reg = new();

    /// <summary>
    /// Build a mob entity with stat fields set to easily-checkable
    /// values. We use a mob (not a PC) because the stat-mod handlers
    /// only touch <see cref="Entity.Stats"/> — entity type doesn't
    /// matter for OnStart/OnEnd behavior.
    /// </summary>
    private static MobEntity MakeTarget(short str = 50, short agi = 50, short vit = 50, short luk = 50,
        short hit = 100, short flee = 100, short cri = 100, ushort batk = 100,
        int maxHp = 1000, short patk = 50)
    {
        var db = new Map.Server.Mob.MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring" };
        var mob = new MobEntity(new EntityId(1), 1002, "Poring", mapId: 0, x: 0, y: 0);
        mob.Stats.Str = str;
        mob.Stats.Agi = agi;
        mob.Stats.Vit = vit;
        mob.Stats.Luk = luk;
        mob.Stats.Hit = hit;
        mob.Stats.Flee = flee;
        mob.Stats.Cri = cri;
        mob.Stats.Batk = batk;
        mob.Stats.MaxHp = maxHp;
        mob.Stats.Hp = maxHp;
        mob.Stats.Patk = patk;
        return mob;
    }

    private static StatusChange MakeSc(int val1 = 1, int val2 = 0, int val3 = 0, int val4 = 0,
        StatusType type = StatusType.Blessing)
        => new() { Type = type, Val1 = val1, Val2 = val2, Val3 = val3, Val4 = val4 };

    private static StatusEffectHandler H(StatusType t) => _reg.Get(t)!;

    // ===== SC_BLIND =====

    [Fact]
    public void Blind_subtracts_quarter_of_Hit_and_Flee_and_reverts()
    {
        var mob = MakeTarget(hit: 100, flee: 200);
        var sc = MakeSc();
        H(StatusType.Blind).OnStart(mob, sc, null);
        Assert.Equal(75, mob.Stats.Hit);   // 100 − 25
        Assert.Equal(150, mob.Stats.Flee); // 200 − 50
        Assert.Equal(25, sc.Val2);
        Assert.Equal(50, sc.Val3);
        H(StatusType.Blind).OnEnd(mob, sc);
        Assert.Equal(100, mob.Stats.Hit);
        Assert.Equal(200, mob.Stats.Flee);
    }

    [Fact]
    public void Blind_flagged_as_debuff_with_RemoveOnRefresh()
    {
        var flags = _reg.GetEffectiveFlags(StatusType.Blind);
        Assert.True(flags.HasFlag(ScfFlag.Debuff));
        Assert.True(flags.HasFlag(ScfFlag.RemoveOnRefresh));
    }

    // ===== SC_CURSE =====

    [Fact]
    public void Curse_zeroes_Luk_and_drops_Batk_quarter_then_restores()
    {
        var mob = MakeTarget(luk: 80, batk: 200);
        var sc = MakeSc();
        H(StatusType.Curse).OnStart(mob, sc, null);
        Assert.Equal(0, mob.Stats.Luk);
        Assert.Equal(150, mob.Stats.Batk);  // 200 − 50
        Assert.Equal(80, sc.Val2);
        Assert.Equal(50, sc.Val3);
        H(StatusType.Curse).OnEnd(mob, sc);
        Assert.Equal(80, mob.Stats.Luk);
        Assert.Equal(200, mob.Stats.Batk);
    }

    [Fact]
    public void Curse_with_zero_luk_immunity_is_no_op()
    {
        // rAthena status.cpp:9472 — Luk=0 ⇒ immune. Handler must not
        // mutate (Batk stays unchanged) and Val2/Val3 stay 0 so OnEnd
        // is also a no-op.
        var mob = MakeTarget(luk: 0, batk: 200);
        var sc = MakeSc();
        H(StatusType.Curse).OnStart(mob, sc, null);
        Assert.Equal(0, mob.Stats.Luk);
        Assert.Equal(200, mob.Stats.Batk); // unchanged
        Assert.Equal(0, sc.Val2);
        Assert.Equal(0, sc.Val3);
        H(StatusType.Curse).OnEnd(mob, sc);
        Assert.Equal(200, mob.Stats.Batk); // still unchanged
    }

    // ===== SC_WINDWALK =====

    [Fact]
    public void WindWalk_lv5_adds_3_flee_and_3_AspdRate()
    {
        // val1=5 → (5+1)/2 = 3 bonus per rAthena status.cpp:10985.
        var mob = MakeTarget(flee: 100);
        var sc = MakeSc(val1: 5);
        var aspdBefore = mob.Stats.AspdRate;
        H(StatusType.Windwalk).OnStart(mob, sc, null);
        Assert.Equal(103, mob.Stats.Flee);
        Assert.Equal((short)(aspdBefore + 3), mob.Stats.AspdRate);
        H(StatusType.Windwalk).OnEnd(mob, sc);
        Assert.Equal(100, mob.Stats.Flee);
        Assert.Equal(aspdBefore, mob.Stats.AspdRate);
    }

    [Theory]
    [InlineData(1, 1)] [InlineData(2, 1)] [InlineData(3, 2)] [InlineData(4, 2)]
    [InlineData(5, 3)] [InlineData(6, 3)] [InlineData(7, 4)] [InlineData(8, 4)]
    [InlineData(9, 5)] [InlineData(10, 5)]
    public void WindWalk_bonus_table_matches_rAthena(int lvl, int expectedBonus)
    {
        var mob = MakeTarget(flee: 0);
        var sc = MakeSc(val1: lvl);
        H(StatusType.Windwalk).OnStart(mob, sc, null);
        Assert.Equal(expectedBonus, mob.Stats.Flee);
    }

    // ===== SC_BERSERK =====

    [Fact]
    public void Berserk_applies_full_buff_combo_and_reverts()
    {
        // Wave 97-2 — rAthena consumer reads (status.cpp:3206-3207 +200% MaxHp;
        // 7678-7679 -50% Flee; 7752/7865/7927/7989 Def/Def2/Mdef/Mdef2 → 0;
        // aspd_rate -= 300).  Our port keeps the Batk +200 approximation (no
        // skillratio SC hook).
        var mob = MakeTarget(flee: 100, batk: 100, maxHp: 1000);
        mob.Stats.Hp = 500;
        mob.Stats.Def = 80; mob.Stats.Def2 = 40;
        mob.Stats.Mdef = 30; mob.Stats.Mdef2 = 20;
        var sc = MakeSc();
        var aspdBefore = mob.Stats.AspdRate;
        H(StatusType.Berserk).OnStart(mob, sc, null);
        Assert.Equal(300, mob.Stats.Batk);
        Assert.Equal(50, mob.Stats.Flee);                 // halved
        Assert.Equal((short)(aspdBefore + 30), mob.Stats.AspdRate);
        Assert.Equal(3000, mob.Stats.MaxHp);
        Assert.Equal(3000, mob.Stats.Hp);
        Assert.Equal(2000, sc.Val2);
        Assert.Equal(0, mob.Stats.Def);                   // zero'd
        Assert.Equal(0, mob.Stats.Def2);
        Assert.Equal(0, mob.Stats.Mdef);
        Assert.Equal(0, mob.Stats.Mdef2);
        H(StatusType.Berserk).OnEnd(mob, sc);
        Assert.Equal(100, mob.Stats.Batk);
        Assert.Equal(100, mob.Stats.Flee);
        Assert.Equal(aspdBefore, mob.Stats.AspdRate);
        Assert.Equal(1000, mob.Stats.MaxHp);
        Assert.Equal(1000, mob.Stats.Hp);
        Assert.Equal(80, mob.Stats.Def);                  // restored
        Assert.Equal(40, mob.Stats.Def2);
        Assert.Equal(30, mob.Stats.Mdef);
        Assert.Equal(20, mob.Stats.Mdef2);
    }

    // ===== SC_LAUDAAGNUS =====

    [Fact]
    public void LaudaAgnus_lv3_adds_12_vit()
    {
        // 4 × val1 per rAthena Lauda Agnus side-effect.
        var mob = MakeTarget(vit: 50);
        var sc = MakeSc(val1: 3);
        H(StatusType.Laudaagnus).OnStart(mob, sc, null);
        Assert.Equal(62, mob.Stats.Vit);
        H(StatusType.Laudaagnus).OnEnd(mob, sc);
        Assert.Equal(50, mob.Stats.Vit);
    }

    // ===== SC_LAUDARAMUS =====

    [Fact]
    public void LaudaRamus_lv2_adds_6_cri_at_10x_storage()
    {
        // 3 × val1 critical chance; stored at 10× display → +60.
        var mob = MakeTarget(cri: 100);
        var sc = MakeSc(val1: 2);
        H(StatusType.Laudaramus).OnStart(mob, sc, null);
        Assert.Equal(160, mob.Stats.Cri);
        H(StatusType.Laudaramus).OnEnd(mob, sc);
        Assert.Equal(100, mob.Stats.Cri);
    }

    // ===== SC_IMPOSITIO =====

    [Fact]
    public void Impositio_lv5_adds_25_Batk()
    {
        // Val1*5 per rAthena status.cpp:10368 (Impositio).
        var mob = MakeTarget(batk: 100);
        var sc = MakeSc(val1: 5);
        H(StatusType.Impositio).OnStart(mob, sc, null);
        Assert.Equal(125, mob.Stats.Batk);
        Assert.Equal(25, sc.Val2);
        H(StatusType.Impositio).OnEnd(mob, sc);
        Assert.Equal(100, mob.Stats.Batk);
    }

    // ===== SC_ADORAMUS =====

    [Fact]
    public void Adoramus_drops_Agi_by_val1_and_reverts()
    {
        var mob = MakeTarget(agi: 60);
        var sc = MakeSc(val1: 8);
        H(StatusType.Adoramus).OnStart(mob, sc, null);
        Assert.Equal(52, mob.Stats.Agi);
        H(StatusType.Adoramus).OnEnd(mob, sc);
        Assert.Equal(60, mob.Stats.Agi);
    }

    // ===== SC_DRAGONIC_AURA =====

    [Fact]
    public void DragonicAura_lv4_adds_40_Patk_and_20_Hit()
    {
        var mob = MakeTarget(hit: 100, patk: 50);
        var sc = MakeSc(val1: 4);
        H(StatusType.DragonicAura).OnStart(mob, sc, null);
        Assert.Equal(90, mob.Stats.Patk);  // 50 + 40
        Assert.Equal(120, mob.Stats.Hit);  // 100 + 20
        H(StatusType.DragonicAura).OnEnd(mob, sc);
        Assert.Equal(50, mob.Stats.Patk);
        Assert.Equal(100, mob.Stats.Hit);
    }

    // ===== SC_CARTBOOST =====

    [Fact]
    public void CartBoost_adds_20_AspdRate_and_reverts()
    {
        var mob = MakeTarget();
        var sc = MakeSc(val1: 1);
        var aspdBefore = mob.Stats.AspdRate;
        H(StatusType.Cartboost).OnStart(mob, sc, null);
        Assert.Equal((short)(aspdBefore + 20), mob.Stats.AspdRate);
        H(StatusType.Cartboost).OnEnd(mob, sc);
        Assert.Equal(aspdBefore, mob.Stats.AspdRate);
    }

    // ===== Combat-marker flag reclassification (NS-3 wave 1 second batch) =====

    [Theory]
    [InlineData(StatusType.Overthrust)]
    [InlineData(StatusType.Maximizepower)]
    [InlineData(StatusType.Magicpower)]
    [InlineData(StatusType.Tensionrelax)]
    [InlineData(StatusType.Hiding)]
    [InlineData(StatusType.Cloaking)]
    [InlineData(StatusType.Kaite)]
    [InlineData(StatusType.Providence)]
    public void CombatBuffMarkers_are_classified_as_buff_with_RemoveOnLogout(StatusType type)
    {
        var flags = _reg.GetEffectiveFlags(type);
        Assert.True(flags.HasFlag(ScfFlag.Buff),
            $"{type} expected Buff flag, got {flags}");
        Assert.True(flags.HasFlag(ScfFlag.RemoveOnLogout),
            $"{type} expected RemoveOnLogout, got {flags}");
    }

    [Theory]
    [InlineData(StatusType.Aeterna)]
    [InlineData(StatusType.Signumcrucis)]
    public void CombatDebuffMarkers_are_classified_as_debuff_with_RemoveOnRefresh(StatusType type)
    {
        var flags = _reg.GetEffectiveFlags(type);
        Assert.True(flags.HasFlag(ScfFlag.Debuff),
            $"{type} expected Debuff flag, got {flags}");
        Assert.True(flags.HasFlag(ScfFlag.RemoveOnRefresh),
            $"{type} expected RemoveOnRefresh, got {flags}");
    }
}
