namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_map_property</c> [clif.cpp:10828]. Tells the client
/// which "map property" mode it's in (PvP, GvG, BG, etc.).
/// Fixed 8 bytes: <c>0x099b packet_id (2) + type (2) + flag (4)</c>.
/// </summary>
public class ZC_MAPPROPERTY_R2 : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short) + sizeof(uint);

    public short MapType { get; init; }
    public uint Flag { get; init; }

    public ZC_MAPPROPERTY_R2() : base(PacketHeader.ZC_MAPPROPERTY_R2, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(MapType);
        writer.Write(Flag);
    }
}
