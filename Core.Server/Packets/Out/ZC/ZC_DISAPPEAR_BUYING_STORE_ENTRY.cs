namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Remove a buying-store sign. rAthena <c>clif_buyingstore_disappear_entry</c> (clif.cpp, 0x0816).
/// Fixed 6 bytes: <c>0816 &lt;maker AID&gt;.L</c>. Broadcast to the area when the store closes.
/// </summary>
public class ZC_DISAPPEAR_BUYING_STORE_ENTRY : OutgoingPacket
{
    private const int SIZE = 2 + 4; // 6

    public uint MakerAccountId { get; init; }

    public ZC_DISAPPEAR_BUYING_STORE_ENTRY() : base(PacketHeader.ZC_DISAPPEAR_BUYING_STORE_ENTRY, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(MakerAccountId);
}
