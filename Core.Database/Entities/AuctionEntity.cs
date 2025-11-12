namespace Core.Database.Entities;

public class AuctionEntity
{
    public long AuctionId { get; set; }
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public uint Price { get; set; }
    public uint Buynow { get; set; }
    public short Hours { get; set; }
    public uint Timestamp { get; set; }
    public uint NameId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public short Type { get; set; }
    public byte Refine { get; set; }
    public byte Attribute { get; set; }
    public uint Card0 { get; set; }
    public uint Card1 { get; set; }
    public uint Card2 { get; set; }
    public uint Card3 { get; set; }
    public short OptionId0 { get; set; }
    public short OptionVal0 { get; set; }
    public sbyte OptionParm0 { get; set; }
    public short OptionId1 { get; set; }
    public short OptionVal1 { get; set; }
    public sbyte OptionParm1 { get; set; }
    public short OptionId2 { get; set; }
    public short OptionVal2 { get; set; }
    public sbyte OptionParm2 { get; set; }
    public short OptionId3 { get; set; }
    public short OptionVal3 { get; set; }
    public sbyte OptionParm3 { get; set; }
    public short OptionId4 { get; set; }
    public short OptionVal4 { get; set; }
    public sbyte OptionParm4 { get; set; }
    public ulong UniqueId { get; set; }
    public byte EnchantGrade { get; set; }
}

