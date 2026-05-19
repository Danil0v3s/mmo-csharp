namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_tradeadditem</c> (clif.cpp:4745) — notifies the
/// trade partner about an item this side added. Wire (legacy 0x00e9):
/// <c>&lt;amount&gt;.L &lt;nameid&gt;.W &lt;identified&gt;.B
/// &lt;damaged&gt;.B &lt;refine&gt;.B &lt;c1&gt;.W &lt;c2&gt;.W
/// &lt;c3&gt;.W &lt;c4&gt;.W</c> — total 2 + 17 = 19 bytes.
/// </summary>
public class ZC_ADD_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 2 + sizeof(int) + sizeof(short) + 3 + 4 * sizeof(short);

    public int Amount { get; init; }
    public ushort NameId { get; init; }
    public byte Identified { get; init; } = 1;
    public byte Damaged { get; init; }
    public byte Refine { get; init; }
    public ushort Card0 { get; init; }
    public ushort Card1 { get; init; }
    public ushort Card2 { get; init; }
    public ushort Card3 { get; init; }

    public ZC_ADD_EXCHANGE_ITEM() : base(PacketHeader.ZC_ADD_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Amount);
        writer.Write(NameId);
        writer.Write(Identified);
        writer.Write(Damaged);
        writer.Write(Refine);
        writer.Write(Card0);
        writer.Write(Card1);
        writer.Write(Card2);
        writer.Write(Card3);
    }
}
