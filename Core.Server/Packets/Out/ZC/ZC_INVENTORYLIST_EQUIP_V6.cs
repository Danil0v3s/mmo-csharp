namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_inventorylist_equip</c> chunk — paired with
/// <see cref="ZC_INVENTORYLIST_NORMAL_V6"/> in the inventory-list
/// stream. Variable length; body is invType (1) + EQUIPITEM_INFO[].
/// </summary>
public class ZC_INVENTORYLIST_EQUIP_V6 : OutgoingPacket
{
    public byte InvType { get; init; }
    public byte[] Body { get; init; } = Array.Empty<byte>();

    public ZC_INVENTORYLIST_EQUIP_V6() : base(PacketHeader.ZC_INVENTORYLIST_EQUIP_V6, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + sizeof(byte) + Body.Length;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(InvType);
        writer.Write(Body);
    }
}
