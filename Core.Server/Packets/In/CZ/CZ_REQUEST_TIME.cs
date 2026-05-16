namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client-side ping. rAthena <c>clif_parse_TickSend</c>.
/// Shape: 0x007e packet_id (2) + clientTick (4) = 6 bytes. Server echoes
/// back via <c>ZC_NOTIFY_TIME</c> so the client can compute latency.
/// </summary>
public class CZ_REQUEST_TIME : IncomingPacket
{
    private const int SIZE = 6;

    public uint ClientTick { get; private set; }

    public CZ_REQUEST_TIME() : base(PacketHeader.CZ_REQUEST_TIME, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientTick = reader.ReadUInt32();
    }

    public static CZ_REQUEST_TIME Create(BinaryReader reader)
    {
        var packet = new CZ_REQUEST_TIME();
        packet.Read(reader);
        return packet;
    }
}
