namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Client clicked the 'Close' button in the open dialog." rAthena
/// <c>clif_parse_CloseScript</c>. Fixed 6 bytes: 0x0146 packet_id (2) +
/// npcId (4). The server finalises the dialog session.
///
/// Note: the OUTGOING packet with the same conceptual purpose
/// (<see cref="Out.ZC.ZC_CLOSE_DIALOG"/>) uses id 0x00b6. The two are
/// distinct on the wire.
/// </summary>
public class CZ_CLOSE_DIALOG : IncomingPacket
{
    private const int SIZE = 6;

    public uint NpcId { get; private set; }

    public CZ_CLOSE_DIALOG() : base(PacketHeader.CZ_CLOSE_DIALOG, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        NpcId = reader.ReadUInt32();
    }

    public static CZ_CLOSE_DIALOG Create(BinaryReader reader)
    {
        var packet = new CZ_CLOSE_DIALOG();
        packet.Read(reader);
        return packet;
    }
}
