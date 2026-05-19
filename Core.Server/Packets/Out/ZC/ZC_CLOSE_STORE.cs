namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server ack that the storage window is closed. rAthena
/// <c>clif_storageclose</c> (clif.cpp:4931). Wire: <c>00f8</c> header
/// only — 2 bytes.
/// </summary>
public class ZC_CLOSE_STORE : OutgoingPacket
{
    private const int SIZE = 2;

    public ZC_CLOSE_STORE() : base(PacketHeader.ZC_CLOSE_STORE, SIZE) { }

    public override void Write(BinaryWriter writer) { /* no body */ }
}
