namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: refresh the inbox list. rAthena <c>clif_parse_Mail_refreshinbox</c> +
/// <c>PACKET_CZ_REQ_REFRESH_MAIL_LIST</c> (packets_struct.hpp, PACKETVER ≥ 20170419). Wire:
/// <c>0ac1 &lt;upper mailID&gt;.Q &lt;unknown&gt;.16B</c> — 26 bytes. At this packetver the server resends the
/// whole inbox, so the fields are read but unused.
/// </summary>
public class CZ_REQ_REFRESH_MAIL_LIST : IncomingPacket
{
    private const int SIZE = 26;

    public long UpperMailId { get; private set; }

    public CZ_REQ_REFRESH_MAIL_LIST() : base(PacketHeader.CZ_REQ_REFRESH_MAIL_LIST, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        UpperMailId = reader.ReadInt64();
        reader.ReadBytes(16); // unknown padding
    }

    public static CZ_REQ_REFRESH_MAIL_LIST Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_REFRESH_MAIL_LIST();
        packet.Read(reader);
        return packet;
    }
}
