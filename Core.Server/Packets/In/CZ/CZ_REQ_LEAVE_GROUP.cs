namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Leave your party. rAthena <c>clif_parse_LeaveParty</c> (clif.cpp). Header-only, 2 bytes
/// (the request carries no body — the session identifies the leaver).
/// </summary>
public class CZ_REQ_LEAVE_GROUP : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_REQ_LEAVE_GROUP() : base(PacketHeader.CZ_REQ_LEAVE_GROUP, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_REQ_LEAVE_GROUP Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_LEAVE_GROUP();
        packet.Read(reader);
        return packet;
    }
}
