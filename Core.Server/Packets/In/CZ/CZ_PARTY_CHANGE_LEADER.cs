namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Hand party leadership to another member (leader only). rAthena
/// <c>clif_parse_PartyChangeLeader</c> (clif.cpp). Wire: <c>07da &lt;account id&gt;.L</c> — 6 bytes.
/// </summary>
public class CZ_PARTY_CHANGE_LEADER : IncomingPacket
{
    private const int SIZE = 6;

    public int AccountId { get; private set; }

    public CZ_PARTY_CHANGE_LEADER() : base(PacketHeader.CZ_PARTY_CHANGE_LEADER, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        AccountId = reader.ReadInt32();
    }

    public static CZ_PARTY_CHANGE_LEADER Create(BinaryReader reader)
    {
        var packet = new CZ_PARTY_CHANGE_LEADER();
        packet.Read(reader);
        return packet;
    }
}
