namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX remove-item-from-mail acknowledgement. rAthena <c>clif_mail_removeitem</c> (clif.cpp) +
/// <c>PACKET_ZC_ACK_REMOVE_ITEM_MAIL</c> (rodexremoveitem). Wire:
/// <c>0a07 &lt;result&gt;.B &lt;index&gt;.W &lt;cnt&gt;.W &lt;weight&gt;.W</c> — 9 bytes. <c>result</c>: 1 = success.
/// </summary>
public class ZC_ACK_REMOVE_ITEM_MAIL : OutgoingPacket
{
    private const int SIZE = 9;

    public bool Success { get; init; }
    public short Index { get; init; }
    public ushort Count { get; init; }
    public short Weight { get; init; }

    public ZC_ACK_REMOVE_ITEM_MAIL() : base(PacketHeader.ZC_ACK_REMOVE_ITEM_MAIL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((byte)(Success ? 1 : 0));
        writer.Write(Index);
        writer.Write(Count);
        writer.Write(Weight);
    }
}
