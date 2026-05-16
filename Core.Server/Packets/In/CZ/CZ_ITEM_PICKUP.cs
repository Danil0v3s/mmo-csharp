namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Take this item off the floor." rAthena <c>clif_parse_TakeItem</c>.
/// PACKETVER 20211103 uses the <c>CZ_ITEM_PICKUP2</c> variant (0x0362):
/// 0x0362 packet_id (2) + entityId (4) = 6 bytes.
/// </summary>
public class CZ_ITEM_PICKUP : IncomingPacket
{
    private const int SIZE = 6;

    public int ItemEntityId { get; private set; }

    public CZ_ITEM_PICKUP() : base(PacketHeader.CZ_ITEM_PICKUP, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ItemEntityId = reader.ReadInt32();
    }

    public static CZ_ITEM_PICKUP Create(BinaryReader reader)
    {
        var packet = new CZ_ITEM_PICKUP();
        packet.Read(reader);
        return packet;
    }
}
