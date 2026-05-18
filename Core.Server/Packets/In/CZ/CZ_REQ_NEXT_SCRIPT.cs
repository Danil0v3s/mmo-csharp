namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Client clicked the 'Next' button in the open dialog." rAthena
/// <c>clif_parse_NextScript</c>. Fixed 6 bytes: 0x00b9 packet_id (2) +
/// npcId (4). The server resumes the suspended script at its current
/// <c>await ctx.next()</c>.
/// </summary>
public class CZ_REQ_NEXT_SCRIPT : IncomingPacket
{
    private const int SIZE = 6;

    public uint NpcId { get; private set; }

    public CZ_REQ_NEXT_SCRIPT() : base(PacketHeader.CZ_REQ_NEXT_SCRIPT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        NpcId = reader.ReadUInt32();
    }

    public static CZ_REQ_NEXT_SCRIPT Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_NEXT_SCRIPT();
        packet.Read(reader);
        return packet;
    }
}
