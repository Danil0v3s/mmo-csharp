using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AuctionEntityConfiguration : IEntityTypeConfiguration<AuctionEntity>
{
    public void Configure(EntityTypeBuilder<AuctionEntity> builder)
    {
        builder.ToTable("auction");
        builder.HasKey(e => e.AuctionId);
        
        builder.Property(e => e.AuctionId).HasColumnName("auction_id");
        builder.Property(e => e.SellerId).HasColumnName("seller_id").HasDefaultValue(0u);
        builder.Property(e => e.SellerName).HasColumnName("seller_name").HasMaxLength(30).IsRequired().HasDefaultValue("");
        builder.Property(e => e.BuyerId).HasColumnName("buyer_id").HasDefaultValue(0u);
        builder.Property(e => e.BuyerName).HasColumnName("buyer_name").HasMaxLength(30).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Price).HasColumnName("price").HasDefaultValue(0u);
        builder.Property(e => e.Buynow).HasColumnName("buynow").HasDefaultValue(0u);
        builder.Property(e => e.Hours).HasColumnName("hours").HasDefaultValue((short)0);
        builder.Property(e => e.Timestamp).HasColumnName("timestamp").HasDefaultValue(0u);
        builder.Property(e => e.NameId).HasColumnName("nameid").HasDefaultValue(0u);
        builder.Property(e => e.ItemName).HasColumnName("item_name").HasMaxLength(50).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Type).HasColumnName("type").HasDefaultValue((short)0);
        builder.Property(e => e.Refine).HasColumnName("refine").HasDefaultValue((byte)0);
        builder.Property(e => e.Attribute).HasColumnName("attribute").HasDefaultValue((byte)0);
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
        builder.Property(e => e.UniqueId).HasColumnName("unique_id").HasDefaultValue(0ul);
        builder.Property(e => e.EnchantGrade).HasColumnName("enchantgrade").HasDefaultValue((byte)0);
    }
}
