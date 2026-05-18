namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_additem</c> (clif.cpp:2832). Fires whenever an item
/// enters the player's inventory — pickup, NPC gift, trade complete,
/// shop purchase, refine return, etc. Fixed 70 bytes for
/// PACKETVER_RE_NUM >= 20200723 (our live-client baseline). See
/// rAthena <c>PACKET_ZC_ITEM_PICKUP_ACK</c> in <c>packets_struct.hpp</c>.
///
/// Body layout for our PACKETVER:
/// <code>
///   uint16 index           2   server_slot + 2
///   uint16 count           2   amount added in this event
///   uint32 nameid          4
///   uint8  IsIdentified    1
///   uint8  IsDamaged       1   (attribute > 0 = broken)
///   uint32 card[4]        16
///   uint32 location        4   equippable-slot bits (0 for non-equip)
///   uint8  type            1   IT_* enum
///   uint8  result          1   0 = success; non-zero = fail reason
///   int32  HireExpireDate  4   0 = no expiry
///   uint16 bindOnEquipType 2   0 = none, 1 = char, 2 = account
///   ItemOptions opts[5]   25   (id:2, value:2, param:1) × 5
///   uint8  favorite        1
///   uint16 look            2   item-db look override (0 = use db)
///   uint8  refiningLevel   1
///   uint8  grade           1
/// </code>
/// </summary>
public class ZC_ITEM_PICKUP_ACK : OutgoingPacket
{
    private const int SIZE = 70;

    public short Index { get; init; }
    public short Count { get; init; }
    public uint NameId { get; init; }
    public byte IsIdentified { get; init; } = 1;
    public byte IsDamaged { get; init; }
    public uint Card0 { get; init; }
    public uint Card1 { get; init; }
    public uint Card2 { get; init; }
    public uint Card3 { get; init; }
    public uint Location { get; init; }
    public byte Type { get; init; }
    public byte Result { get; init; }
    public int HireExpireDate { get; init; }
    public ushort BindOnEquipType { get; init; }
    public OptionTuple Option0 { get; init; }
    public OptionTuple Option1 { get; init; }
    public OptionTuple Option2 { get; init; }
    public OptionTuple Option3 { get; init; }
    public OptionTuple Option4 { get; init; }
    public byte Favorite { get; init; }
    public ushort Look { get; init; }
    public byte RefiningLevel { get; init; }
    public byte EnchantGrade { get; init; }

    public ZC_ITEM_PICKUP_ACK() : base(PacketHeader.ZC_ITEM_PICKUP_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Count);
        writer.Write(NameId);
        writer.Write(IsIdentified);
        writer.Write(IsDamaged);
        writer.Write(Card0);
        writer.Write(Card1);
        writer.Write(Card2);
        writer.Write(Card3);
        writer.Write(Location);
        writer.Write(Type);
        writer.Write(Result);
        writer.Write(HireExpireDate);
        writer.Write(BindOnEquipType);
        WriteOption(writer, Option0);
        WriteOption(writer, Option1);
        WriteOption(writer, Option2);
        WriteOption(writer, Option3);
        WriteOption(writer, Option4);
        writer.Write(Favorite);
        writer.Write(Look);
        writer.Write(RefiningLevel);
        writer.Write(EnchantGrade);
    }

    private static void WriteOption(BinaryWriter writer, OptionTuple opt)
    {
        writer.Write(opt.Id);
        writer.Write(opt.Value);
        writer.Write(opt.Param);
    }

    public readonly record struct OptionTuple(short Id, short Value, sbyte Param);
}
