using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CartInventoryEntityConfiguration : IEntityTypeConfiguration<CartInventoryEntity>
{
    public void Configure(EntityTypeBuilder<CartInventoryEntity> builder)
    {
        builder.ToTable("cart_inventory");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.NameId).HasColumnName("nameid").HasDefaultValue(0u);
        builder.Property(e => e.Amount).HasColumnName("amount").HasDefaultValue(0);
        builder.Property(e => e.Equip).HasColumnName("equip").HasDefaultValue(0u);
        builder.Property(e => e.Identify).HasColumnName("identify").HasDefaultValue((short)0);
        builder.Property(e => e.Refine).HasColumnName("refine").HasDefaultValue((byte)0);
        builder.Property(e => e.Attribute).HasColumnName("attribute").HasDefaultValue((sbyte)0);
        builder.Property(e => e.Card0).HasColumnName("card0").HasDefaultValue(0u);
        builder.Property(e => e.Card1).HasColumnName("card1").HasDefaultValue(0u);
        builder.Property(e => e.Card2).HasColumnName("card2").HasDefaultValue(0u);
        builder.Property(e => e.Card3).HasColumnName("card3").HasDefaultValue(0u);
        builder.Property(e => e.OptionId0).HasColumnName("option_id0").HasDefaultValue((short)0);
        builder.Property(e => e.OptionVal0).HasColumnName("option_val0").HasDefaultValue((short)0);
        builder.Property(e => e.OptionParm0).HasColumnName("option_parm0").HasDefaultValue((sbyte)0);
        builder.Property(e => e.OptionId1).HasColumnName("option_id1").HasDefaultValue((short)0);
        builder.Property(e => e.OptionVal1).HasColumnName("option_val1").HasDefaultValue((short)0);
        builder.Property(e => e.OptionParm1).HasColumnName("option_parm1").HasDefaultValue((sbyte)0);
        builder.Property(e => e.OptionId2).HasColumnName("option_id2").HasDefaultValue((short)0);
        builder.Property(e => e.OptionVal2).HasColumnName("option_val2").HasDefaultValue((short)0);
        builder.Property(e => e.OptionParm2).HasColumnName("option_parm2").HasDefaultValue((sbyte)0);
        builder.Property(e => e.OptionId3).HasColumnName("option_id3").HasDefaultValue((short)0);
        builder.Property(e => e.OptionVal3).HasColumnName("option_val3").HasDefaultValue((short)0);
        builder.Property(e => e.OptionParm3).HasColumnName("option_parm3").HasDefaultValue((sbyte)0);
        builder.Property(e => e.OptionId4).HasColumnName("option_id4").HasDefaultValue((short)0);
        builder.Property(e => e.OptionVal4).HasColumnName("option_val4").HasDefaultValue((short)0);
        builder.Property(e => e.OptionParm4).HasColumnName("option_parm4").HasDefaultValue((sbyte)0);
        builder.Property(e => e.ExpireTime).HasColumnName("expire_time").HasDefaultValue(0u);
        builder.Property(e => e.Bound).HasColumnName("bound").HasDefaultValue((byte)0);
        builder.Property(e => e.UniqueId).HasColumnName("unique_id").HasDefaultValue(0ul);
        builder.Property(e => e.EnchantGrade).HasColumnName("enchantgrade").HasDefaultValue((byte)0);
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
