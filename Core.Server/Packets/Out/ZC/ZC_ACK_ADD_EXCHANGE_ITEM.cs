namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_tradeitemok</c> (clif.cpp:4795) — server's ack to
/// the client's add-item request. Wire (0x00ea):
/// <c>&lt;index&gt;.W &lt;result&gt;.B</c> — total 5 bytes.
///
/// Result codes:
/// <list type="bullet">
///   <item>0 = success</item>
///   <item>1 = overweight</item>
///   <item>2 = trade canceled</item>
/// </list>
/// </summary>
public class ZC_ACK_ADD_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 2 + sizeof(short) + 1;

    public ushort Index { get; init; }
    public byte Result { get; init; }

    public ZC_ACK_ADD_EXCHANGE_ITEM() : base(PacketHeader.ZC_ACK_ADD_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Result);
    }
}
