using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

// EF table mappings for the second wave of catalog seeds. Bundled
// to keep boilerplate dense — every config is < 15 lines.

public class CastleDbEntityConfiguration : IEntityTypeConfiguration<CastleDbEntity>
{
    public void Configure(EntityTypeBuilder<CastleDbEntity> b)
    {
        b.ToTable("castle_db");
        b.HasKey(e => e.CastleId);
        b.Property(e => e.CastleId).HasColumnName("castle_id");
        b.Property(e => e.MapName).HasColumnName("map_name").HasMaxLength(64);
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(128);
        b.Property(e => e.NpcName).HasColumnName("npc_name").HasMaxLength(128);
        b.Property(e => e.CastleType).HasColumnName("castle_type").HasMaxLength(32);
        b.Property(e => e.ClientId).HasColumnName("client_id");
        b.Property(e => e.WarpEnabled).HasColumnName("warp_enabled");
        b.Property(e => e.WarpX).HasColumnName("warp_x");
        b.Property(e => e.WarpY).HasColumnName("warp_y");
    }
}

public class StatPointEntityConfiguration : IEntityTypeConfiguration<StatPointEntity>
{
    public void Configure(EntityTypeBuilder<StatPointEntity> b)
    {
        b.ToTable("statpoint");
        b.HasKey(e => e.Level);
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Points).HasColumnName("points");
        b.Property(e => e.TraitPoints).HasColumnName("trait_points");
    }
}

public class ExpHomunEntityConfiguration : IEntityTypeConfiguration<ExpHomunEntity>
{
    public void Configure(EntityTypeBuilder<ExpHomunEntity> b)
    {
        b.ToTable("exp_homun");
        b.HasKey(e => e.Level);
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Exp).HasColumnName("exp");
    }
}

public class ExpGuildEntityConfiguration : IEntityTypeConfiguration<ExpGuildEntity>
{
    public void Configure(EntityTypeBuilder<ExpGuildEntity> b)
    {
        b.ToTable("exp_guild");
        b.HasKey(e => e.Level);
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Exp).HasColumnName("exp");
    }
}

public class SizeFixEntityConfiguration : IEntityTypeConfiguration<SizeFixEntity>
{
    public void Configure(EntityTypeBuilder<SizeFixEntity> b)
    {
        b.ToTable("size_fix");
        b.HasKey(e => e.Weapon);
        b.Property(e => e.Weapon).HasColumnName("weapon").HasMaxLength(32);
        b.Property(e => e.Small).HasColumnName("small");
        b.Property(e => e.Medium).HasColumnName("medium");
        b.Property(e => e.Large).HasColumnName("large");
    }
}

public class ReputationEntityConfiguration : IEntityTypeConfiguration<ReputationEntity>
{
    public void Configure(EntityTypeBuilder<ReputationEntity> b)
    {
        b.ToTable("reputation");
        b.HasKey(e => e.ReputationId);
        b.Property(e => e.ReputationId).HasColumnName("reputation_id");
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(128);
        b.Property(e => e.Variable).HasColumnName("variable").HasMaxLength(64);
        b.Property(e => e.Minimum).HasColumnName("minimum");
        b.Property(e => e.Maximum).HasColumnName("maximum");
    }
}

public class CreateArrowDbEntityConfiguration : IEntityTypeConfiguration<CreateArrowDbEntity>
{
    public void Configure(EntityTypeBuilder<CreateArrowDbEntity> b)
    {
        b.ToTable("create_arrow_db");
        b.HasKey(e => e.SourceItem);
        b.Property(e => e.SourceItem).HasColumnName("source_item").HasMaxLength(64);
        b.Property(e => e.MakeJson).HasColumnName("make_json").HasColumnType("text");
    }
}

public class ItemRandomOptDbEntityConfiguration : IEntityTypeConfiguration<ItemRandomOptDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemRandomOptDbEntity> b)
    {
        b.ToTable("item_randomopt_db");
        b.HasKey(e => e.OptionId);
        b.Property(e => e.OptionId).HasColumnName("option_id");
        b.Property(e => e.OptionName).HasColumnName("option_name").HasMaxLength(64);
        b.Property(e => e.Script).HasColumnName("script").HasColumnType("text");
    }
}

public class CashShopDbEntityConfiguration : IEntityTypeConfiguration<CashShopDbEntity>
{
    public void Configure(EntityTypeBuilder<CashShopDbEntity> b)
    {
        b.ToTable("cashshop_db");
        b.HasKey(e => e.Tab);
        b.Property(e => e.Tab).HasColumnName("tab").HasMaxLength(32);
        b.Property(e => e.ItemsJson).HasColumnName("items_json").HasColumnType("text");
    }
}

public class CaptchaDbEntityConfiguration : IEntityTypeConfiguration<CaptchaDbEntity>
{
    public void Configure(EntityTypeBuilder<CaptchaDbEntity> b)
    {
        b.ToTable("captcha_db");
        b.HasKey(e => e.CaptchaId);
        b.Property(e => e.CaptchaId).HasColumnName("captcha_id");
        b.Property(e => e.Image).HasColumnName("image").HasMaxLength(255);
        b.Property(e => e.Answer).HasColumnName("answer").HasMaxLength(128);
    }
}

// ---- payload-shape table configs ----

internal static class PayloadConfig
{
    public static void StringKey<T>(EntityTypeBuilder<T> b, string table, string keyColumn) where T : PayloadStringKeyEntity
    {
        b.ToTable(table);
        b.HasKey(e => e.Key);
        b.Property(e => e.Key).HasColumnName(keyColumn).HasMaxLength(64).IsRequired();
        b.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("longtext").IsRequired();
    }

    public static void IntKey<T>(EntityTypeBuilder<T> b, string table, string keyColumn) where T : PayloadIntKeyEntity
    {
        b.ToTable(table);
        b.HasKey(e => e.Key);
        b.Property(e => e.Key).HasColumnName(keyColumn);
        b.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("longtext").IsRequired();
    }

    public static void RowIndex<T>(EntityTypeBuilder<T> b, string table) where T : PayloadRowIndexEntity
    {
        b.ToTable(table);
        b.HasKey(e => e.RowId);
        b.Property(e => e.RowId).HasColumnName("row_id");
        b.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("longtext").IsRequired();
    }
}

public class ElementalDbEntityConfiguration : IEntityTypeConfiguration<ElementalDbEntity> { public void Configure(EntityTypeBuilder<ElementalDbEntity> b) => PayloadConfig.IntKey(b, "elemental_db", "ele_id"); }
public class BattlegroundDbEntityConfiguration : IEntityTypeConfiguration<BattlegroundDbEntity> { public void Configure(EntityTypeBuilder<BattlegroundDbEntity> b) => PayloadConfig.IntKey(b, "battleground_db", "bg_id"); }
// SkillTreeEntityConfiguration / GuildSkillTreeEntityConfiguration removed in DB-8c.
// MobSummonEntityConfiguration removed in DB-8b.
public class ItemRandomOptGroupEntityConfiguration : IEntityTypeConfiguration<ItemRandomOptGroupEntity> { public void Configure(EntityTypeBuilder<ItemRandomOptGroupEntity> b) => PayloadConfig.IntKey(b, "item_randomopt_group", "group_id"); }
// AttrFixEntityConfiguration / LevelPenaltyEntityConfiguration removed
// in DB-8a — see StaticDbConfigurations.cs for the typed replacements.
// JobStatsEntityConfiguration / JobExpEntityConfiguration / JobBasePointsEntityConfiguration
// removed in DB-8d.
public class StatusYmlEntityConfiguration : IEntityTypeConfiguration<StatusYmlEntity> { public void Configure(EntityTypeBuilder<StatusYmlEntity> b) => PayloadConfig.StringKey(b, "status_yml", "status_name"); }
// ItemCombosEntityConfiguration / ItemPackagesEntityConfiguration / ItemGroupDbEntityConfiguration
// removed in DB-8b.
public class ItemEnchantEntityConfiguration : IEntityTypeConfiguration<ItemEnchantEntity> { public void Configure(EntityTypeBuilder<ItemEnchantEntity> b) => PayloadConfig.IntKey(b, "item_enchant", "enchant_id"); }
public class ItemReformEntityConfiguration : IEntityTypeConfiguration<ItemReformEntity> { public void Configure(EntityTypeBuilder<ItemReformEntity> b) => PayloadConfig.StringKey(b, "item_reform", "item_aegis"); }
public class LaphineSynthesisEntityConfiguration : IEntityTypeConfiguration<LaphineSynthesisEntity> { public void Configure(EntityTypeBuilder<LaphineSynthesisEntity> b) => PayloadConfig.StringKey(b, "laphine_synthesis", "recipe_item"); }
public class LaphineUpgradeEntityConfiguration : IEntityTypeConfiguration<LaphineUpgradeEntity> { public void Configure(EntityTypeBuilder<LaphineUpgradeEntity> b) => PayloadConfig.StringKey(b, "laphine_upgrade", "upgrade_item"); }
public class RefineEntityConfiguration : IEntityTypeConfiguration<RefineEntity> { public void Configure(EntityTypeBuilder<RefineEntity> b) => PayloadConfig.StringKey(b, "refine", "refine_group"); }
public class EnchantGradeEntityConfiguration : IEntityTypeConfiguration<EnchantGradeEntity> { public void Configure(EntityTypeBuilder<EnchantGradeEntity> b) => PayloadConfig.StringKey(b, "enchantgrade", "equip_type"); }
public class MapDropsEntityConfiguration : IEntityTypeConfiguration<MapDropsEntity> { public void Configure(EntityTypeBuilder<MapDropsEntity> b) => PayloadConfig.StringKey(b, "map_drops", "map_name"); }
public class MobItemRatioEntityConfiguration : IEntityTypeConfiguration<MobItemRatioEntity> { public void Configure(EntityTypeBuilder<MobItemRatioEntity> b) => PayloadConfig.StringKey(b, "mob_item_ratio", "item_aegis"); }
// ItemCashEntityConfiguration / AttendanceDbEntityConfiguration removed in DB-8b.
// ReputationGroupEntityConfiguration removed in DB-8a.
