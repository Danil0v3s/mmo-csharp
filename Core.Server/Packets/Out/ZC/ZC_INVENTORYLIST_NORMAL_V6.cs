namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_inventorylist_normal</c> chunk for PACKETVER_RE_NUM
/// ≥ 20180829. Variable length; carries the consumables / misc items
/// between <see cref="ZC_INVENTORY_START"/> and <see cref="ZC_INVENTORY_END"/>.
///
/// Body shape (per rAthena <c>ZC_STORE_ITEMLIST_NORMAL</c> family):
/// invType (1) + NORMALITEM_INFO[]. Real serialization lands with the
/// item system; for replay-parser purposes the registry only needs the
/// header + variable-length marker.
/// </summary>
public class ZC_INVENTORYLIST_NORMAL_V6 : OutgoingPacket
{
    public byte InvType { get; init; }
    public byte[] Body { get; init; } = Array.Empty<byte>();

    public ZC_INVENTORYLIST_NORMAL_V6() : base(PacketHeader.ZC_INVENTORYLIST_NORMAL_V6, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + sizeof(byte) + Body.Length;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(InvType);
        writer.Write(Body);
    }
}
