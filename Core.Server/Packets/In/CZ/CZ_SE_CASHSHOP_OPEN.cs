namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Open the cash shop UI (the "cash shop" button). rAthena <c>clif_parse_cashshop_open_request</c>
/// (clif.cpp, <c>PACKET_CZ_SE_CASHSHOP_OPEN2</c> 0x0b6d). Fixed 6 bytes: <c>0b6d &lt;tab&gt;.L</c>
/// — the tab the client wants focused.
/// </summary>
public class CZ_SE_CASHSHOP_OPEN : IncomingPacket
{
    private const int SIZE = 6;

    public int Tab { get; private set; }

    public CZ_SE_CASHSHOP_OPEN() : base(PacketHeader.CZ_SE_CASHSHOP_OPEN, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Tab = reader.ReadInt32();
    }

    public static CZ_SE_CASHSHOP_OPEN Create(BinaryReader reader)
    {
        var packet = new CZ_SE_CASHSHOP_OPEN();
        packet.Read(reader);
        return packet;
    }
}
