namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_inventoryend</c>. Terminator for the inventory list
/// stream that <c>clif_inventorylist</c> begins. Fixed 4 bytes:
/// <c>0x0b0b packet_id (2) + invType (1) + flag (1)</c>.
/// </summary>
public class ZC_INVENTORY_END : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte) + sizeof(byte);

    public byte InvType { get; init; }
    public byte Flag { get; init; }

    public ZC_INVENTORY_END() : base(PacketHeader.ZC_INVENTORY_END, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(InvType);
        writer.Write(Flag);
    }
}
