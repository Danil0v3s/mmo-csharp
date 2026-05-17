namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_inventorystart</c> ([packets_struct.hpp:1218]) — opens
/// the inventory streaming block. For PACKETVER_RE_NUM ≥ 20180919 the
/// shape is <c>0x0b08 packet_id (2) + packetLength (2) + invType (1) +
/// name[]</c>. Name is null-terminated and may be empty (inventory has no name).
/// </summary>
public class ZC_INVENTORY_START : OutgoingPacket
{
    public byte InvType { get; init; }
    public string Name { get; init; } = string.Empty;

    public ZC_INVENTORY_START() : base(PacketHeader.ZC_INVENTORY_START, -1) { }

    public override int GetSize()
    {
        // packetType + packetLength + invType + name + null terminator
        return sizeof(short) + sizeof(short) + sizeof(byte) + System.Text.Encoding.ASCII.GetByteCount(Name ?? string.Empty) + 1;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(InvType);
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(Name ?? string.Empty);
        writer.Write(nameBytes);
        writer.Write((byte)0); // null terminator
    }
}
