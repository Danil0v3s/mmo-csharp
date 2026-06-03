using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>One attached item for <see cref="ZC_ACK_READ_RODEX"/> (rAthena PACKET_ZC_ACK_READ_RODEX_SUB).</summary>
public sealed class MailReadItem
{
    public short Count { get; init; }
    public uint ItemId { get; init; }
    public bool Identified { get; init; }
    public bool Damaged { get; init; }
    public sbyte Refine { get; init; }
    public uint Card0 { get; init; }
    public uint Card1 { get; init; }
    public uint Card2 { get; init; }
    public uint Card3 { get; init; }
    public uint Location { get; init; }
    public byte Type { get; init; }
    public ushort ViewSprite { get; init; }
    public ushort BindOnEquip { get; init; }
    /// <summary>5 random-option slots (id, value, param).</summary>
    public (short Id, short Value, sbyte Param)[] Options { get; init; } = new (short, short, sbyte)[5];
}

/// <summary>
/// RODEX read-window. rAthena <c>clif_Mail_read</c> (clif.cpp:16307, PACKETVER ≥ 20150513)
/// + <c>PACKET_ZC_ACK_READ_RODEX</c>. Variable length. Targets the modern struct
/// (uint32 card[4] + uint32 ITID, no-grade SUB). Fixed header (24B):
/// <c>0a02? &lt;len&gt;.W &lt;opentype&gt;.B &lt;mailID&gt;.Q &lt;textLen&gt;.W &lt;zeny&gt;.Q &lt;itemCnt&gt;.B</c>
/// then the body text (textLen, null-terminated), then itemCnt × 59-byte item sub-structs.
/// </summary>
public class ZC_ACK_READ_RODEX : OutgoingPacket
{
    private const int MaxOptions = 5;       // rAthena MAX_ITEM_OPTIONS
    private const int SubSize = 2 + 4 + 1 + 1 + 1 + 16 + 4 + 1 + 2 + 2 + (MaxOptions * 5); // 59

    public byte OpenType { get; init; }
    public long MailId { get; init; }
    public long Zeny { get; init; }
    public string Body { get; init; } = string.Empty;
    public IReadOnlyList<MailReadItem> Items { get; init; } = Array.Empty<MailReadItem>();

    public ZC_ACK_READ_RODEX() : base(PacketHeader.ZC_ACK_READ_RODEX, -1) { }

    private int TextLen => Encoding.ASCII.GetByteCount(Body ?? string.Empty) + 1; // + null terminator

    public override int GetSize()
        // header(2) + len(2) + opentype(1) + mailId(8) + textLen(2) + zeny(8) + itemCnt(1) + body + items
        => 24 + TextLen + Items.Count * SubSize;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(OpenType);
        writer.Write(MailId);
        writer.Write((ushort)TextLen);
        writer.Write(Zeny);
        writer.Write((byte)Items.Count);

        var body = Encoding.ASCII.GetBytes(Body ?? string.Empty);
        writer.Write(body);
        writer.Write((byte)0); // null terminator (TextLen counts it)

        foreach (var it in Items)
        {
            writer.Write(it.Count);
            writer.Write(it.ItemId);
            writer.Write((byte)(it.Identified ? 1 : 0));
            writer.Write((byte)(it.Damaged ? 1 : 0));
            writer.Write(it.Refine);
            writer.Write(it.Card0); writer.Write(it.Card1); writer.Write(it.Card2); writer.Write(it.Card3);
            writer.Write(it.Location);
            writer.Write(it.Type);
            writer.Write(it.ViewSprite);
            writer.Write(it.BindOnEquip);
            for (var i = 0; i < MaxOptions; i++)
            {
                var (id, val, param) = i < it.Options.Length ? it.Options[i] : default;
                writer.Write(id);
                writer.Write(val);
                writer.Write(param);
            }
        }
    }
}
