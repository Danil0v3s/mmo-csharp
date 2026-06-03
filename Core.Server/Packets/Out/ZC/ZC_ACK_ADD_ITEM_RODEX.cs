namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX add-item-to-mail acknowledgement. rAthena <c>clif_Mail_setattachment</c> (clif.cpp) +
/// <c>PACKET_ZC_ACK_ADD_ITEM_RODEX</c> (rodexadditem). Fixed 64 bytes (modern uint32 cards/itemId):
/// <c>0a05 &lt;result&gt;.B &lt;index&gt;.W &lt;count&gt;.W &lt;itemId&gt;.L &lt;type&gt;.B &lt;identified&gt;.B &lt;damaged&gt;.B
/// &lt;cards&gt;.16 &lt;options&gt;.25 &lt;weight&gt;.W &lt;favorite&gt;.B &lt;location&gt;.L &lt;refine&gt;.B &lt;grade&gt;.B</c>.
/// <c>result</c>: 0 = staged OK, non-zero = rejected (mail_setitem flag).
/// </summary>
public class ZC_ACK_ADD_ITEM_RODEX : OutgoingPacket
{
    private const int MaxOptions = 5;
    private const int SIZE = 64; // header(2) + the 62-byte body

    public byte Result { get; init; }
    public short Index { get; init; }
    public short Count { get; init; }
    public uint ItemId { get; init; }
    public byte Type { get; init; }
    public bool Identified { get; init; }
    public bool Damaged { get; init; }
    public uint Card0 { get; init; }
    public uint Card1 { get; init; }
    public uint Card2 { get; init; }
    public uint Card3 { get; init; }
    public (short Id, short Value, sbyte Param)[] Options { get; init; } = new (short, short, sbyte)[5];
    public short Weight { get; init; }
    public byte Favorite { get; init; }
    public uint Location { get; init; }
    public sbyte Refine { get; init; }
    public sbyte Grade { get; init; }

    public ZC_ACK_ADD_ITEM_RODEX() : base(PacketHeader.ZC_ACK_ADD_ITEM_RODEX, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        writer.Write(Index);
        writer.Write(Count);
        writer.Write(ItemId);
        writer.Write(Type);
        writer.Write((byte)(Identified ? 1 : 0));
        writer.Write((byte)(Damaged ? 1 : 0));
        writer.Write(Card0); writer.Write(Card1); writer.Write(Card2); writer.Write(Card3);
        for (var i = 0; i < MaxOptions; i++)
        {
            var (id, val, param) = i < Options.Length ? Options[i] : default;
            writer.Write(id);
            writer.Write(val);
            writer.Write(param);
        }
        writer.Write(Weight);
        writer.Write(Favorite);
        writer.Write(Location);
        writer.Write(Refine);
        writer.Write(Grade);
    }
}
