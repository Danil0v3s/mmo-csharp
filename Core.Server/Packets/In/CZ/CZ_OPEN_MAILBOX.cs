namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: open the mailbox / request the inbox list. rAthena
/// <c>clif_parse_Mail_refreshinbox</c> (clif.cpp:16240) + <c>PACKET_CZ_REQ_OPEN_MAIL</c>
/// (packets_struct.hpp, PACKETVER ≥ 20170419). Wire:
/// <c>0ac0 &lt;char upper mailID&gt;.Q &lt;return upper mailID&gt;.Q &lt;account upper mailID&gt;.Q</c> — 26 bytes.
/// At this packetver the server "always sends all", so the paging upper-ids are read but unused.
/// </summary>
public class CZ_OPEN_MAILBOX : IncomingPacket
{
    private const int SIZE = 26;

    public long CharUpperMailId { get; private set; }
    public long ReturnUpperMailId { get; private set; }
    public long AccountUpperMailId { get; private set; }

    public CZ_OPEN_MAILBOX() : base(PacketHeader.CZ_OPEN_MAILBOX, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        CharUpperMailId = reader.ReadInt64();
        ReturnUpperMailId = reader.ReadInt64();
        AccountUpperMailId = reader.ReadInt64();
    }

    public static CZ_OPEN_MAILBOX Create(BinaryReader reader)
    {
        var packet = new CZ_OPEN_MAILBOX();
        packet.Read(reader);
        return packet;
    }
}
