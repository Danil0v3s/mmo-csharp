namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Storage capacity HUD. rAthena
/// <c>clif_updatestorageamount</c> (clif.cpp:4868). Wire:
/// <c>00f2 &lt;current&gt;.W &lt;max&gt;.W</c> — total 6 bytes.
/// </summary>
public class ZC_NOTIFY_STOREITEM_COUNTINFO : OutgoingPacket
{
    private const int SIZE = 6;

    public ushort Current { get; init; }
    public ushort Max { get; init; }

    public ZC_NOTIFY_STOREITEM_COUNTINFO() : base(PacketHeader.ZC_NOTIFY_STOREITEM_COUNTINFO, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Current);
        writer.Write(Max);
    }
}
