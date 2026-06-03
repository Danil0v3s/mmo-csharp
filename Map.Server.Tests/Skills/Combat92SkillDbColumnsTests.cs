using Core.Database.Entities;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-92 — the skill_db Requirements/Flags/Unit columns (ammo / ammo_amount / inf2 /
/// unit_flags) are parsed by <see cref="SkillDbLoader.FromEntity"/> into the runtime
/// <see cref="SkillDefinition"/>, replacing the retired curated overlays. The importer emits the
/// raw YAML token names; this loader maps ammo names → `1&lt;&lt;AMMO_x` bits and filters flag
/// tokens to the known <see cref="SkillInf2"/> / <see cref="SkillUnitFlag"/> members.
/// </summary>
public class Combat92SkillDbColumnsTests
{
    private const int AmmoArrow = 1 << 1, AmmoBullet = 1 << 3, AmmoShell = 1 << 4,
        AmmoGrenade = 1 << 5, AmmoKunai = 1 << 7;

    private static SkillDefinition Load(string ammo = "", int ammoAmount = 0, string inf2 = "", string unitFlags = "", byte maxLevel = 10)
        => SkillDbLoader.FromEntity(new SkillDbEntity
        {
            Id = 1, Name = "X", MaxLevel = maxLevel,
            TargetMode = "TargetEnemy", DamageKind = "Weapon",
            Ammo = ammo, AmmoAmount = ammoAmount, Inf2 = inf2, UnitFlags = unitFlags,
        });

    [Fact]
    public void Single_ammo_name_maps_to_its_bit_and_broadcasts_qty()
    {
        var def = Load(ammo: "Arrow", ammoAmount: 5, maxLevel: 10);
        Assert.Equal(AmmoArrow, def.AmmoTypeMask);
        Assert.Equal(11, def.AmmoQuantity.Length);  // 1-indexed: 0..MaxLevel
        Assert.Equal(0, def.AmmoQuantity[0]);        // index 0 unused
        Assert.Equal(5, def.AmmoQuantity[1]);
        Assert.Equal(5, def.AmmoQuantity[10]);       // same amount every level (renewal)
    }

    [Fact]
    public void Multiple_ammo_names_are_ord_together()
    {
        var def = Load(ammo: "Bullet|Shell|Grenade", ammoAmount: 1);
        Assert.Equal(AmmoBullet | AmmoShell | AmmoGrenade, def.AmmoTypeMask);
    }

    [Fact]
    public void Kunai_maps_and_qty_two()
    {
        var def = Load(ammo: "Kunai", ammoAmount: 2);
        Assert.Equal(AmmoKunai, def.AmmoTypeMask);
        Assert.Equal(2, def.AmmoQuantity[1]);
    }

    [Fact]
    public void No_ammo_columns_report_zero()
    {
        var def = Load();
        Assert.Equal(0, def.AmmoTypeMask);
        Assert.All(def.AmmoQuantity, q => Assert.Equal(0, q));
    }

    [Fact]
    public void Inf2_tokens_parse_and_unknown_names_are_skipped()
    {
        // The importer emits the full YAML Flags block; FromEntity keeps only known SkillInf2 names.
        var def = Load(inf2: "TargetTrap|IgnoreLandProtector|AlterRangeVulture");
        Assert.True(def.Inf2.HasFlag(SkillInf2.IgnoreLandProtector));
        Assert.False(def.Inf2.HasFlag(SkillInf2.IgnoreGvgReduction));
    }

    [Fact]
    public void Inf2_gvg_and_bg_reduction_parse()
    {
        var def = Load(inf2: "IgnoreGvgReduction|IgnoreBgReduction");
        Assert.True(def.Inf2.HasFlag(SkillInf2.IgnoreGvgReduction));
        Assert.True(def.Inf2.HasFlag(SkillInf2.IgnoreBgReduction));
    }

    [Fact]
    public void Unit_flags_parse_and_unknown_names_are_skipped()
    {
        var def = Load(unitFlags: "NoEnemy|NoReiteration|SomeFutureFlag");
        Assert.True(def.UnitFlags.HasFlag(SkillUnitFlag.NoEnemy));
        Assert.True(def.UnitFlags.HasFlag(SkillUnitFlag.NoReiteration));
        Assert.False(def.UnitFlags.HasFlag(SkillUnitFlag.NoOverlap));
    }

    [Fact]
    public void Empty_flag_columns_yield_none()
    {
        var def = Load();
        Assert.Equal(SkillInf2.None, def.Inf2);
        Assert.Equal(SkillUnitFlag.None, def.UnitFlags);
    }
}
