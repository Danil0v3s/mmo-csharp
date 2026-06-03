namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Expel a member from your party (leader only). rAthena <c>clif_parse_RemovePartyMember</c>
/// (clif.cpp). Wire: <c>0103 &lt;account id&gt;.L &lt;character name&gt;.24B</c> — 30 bytes.
/// </summary>
public class CZ_REQ_EXPEL_GROUP_MEMBER : IncomingPacket
{
    private const int SIZE = 30;

    public int AccountId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public CZ_REQ_EXPEL_GROUP_MEMBER() : base(PacketHeader.CZ_REQ_EXPEL_GROUP_MEMBER, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        AccountId = reader.ReadInt32();
        Name = MailWire.ReadFixedString(reader, 24);
    }

    public static CZ_REQ_EXPEL_GROUP_MEMBER Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_EXPEL_GROUP_MEMBER();
        packet.Read(reader);
        return packet;
    }
}
