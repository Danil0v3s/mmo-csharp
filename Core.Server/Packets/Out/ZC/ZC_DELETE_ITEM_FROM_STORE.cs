namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server confirms an item was taken from storage. rAthena
/// <c>clif_storageitemremoved</c> (clif.cpp:4918). Wire:
/// <c>00f6 &lt;index&gt;.W &lt;amount&gt;.L</c> — total 8 bytes.
/// </summary>
public class ZC_DELETE_ITEM_FROM_STORE : OutgoingPacket
{
    private const int SIZE = 8;

    public ushort ClientIndex { get; init; }
    public int Amount { get; init; }

    public ZC_DELETE_ITEM_FROM_STORE() : base(PacketHeader.ZC_DELETE_ITEM_FROM_STORE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ClientIndex);
        writer.Write(Amount);
    }
}
