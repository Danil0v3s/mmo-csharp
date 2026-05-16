namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client-initiated quit (ALT+E or equivalent). rAthena
/// <c>clif_parse_QuitGame</c>. Shape: 0x018a packet_id (2) + reserved (2) = 4 bytes.
/// </summary>
public class CZ_REQ_QUIT : IncomingPacket
{
    private const int SIZE = 4;

    public CZ_REQ_QUIT() : base(PacketHeader.CZ_REQ_QUIT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        reader.ReadInt16(); // reserved
    }

    public static CZ_REQ_QUIT Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_QUIT();
        packet.Read(reader);
        return packet;
    }
}
