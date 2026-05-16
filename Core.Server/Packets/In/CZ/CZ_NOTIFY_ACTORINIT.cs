namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "LoadEndAck" — client signals it has finished loading the map data and is
/// ready to spawn. rAthena <c>clif_parse_LoadEndAck</c>.
/// Shape: 0x007d packet_id (2). 2 bytes total, no body.
/// </summary>
public class CZ_NOTIFY_ACTORINIT : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_NOTIFY_ACTORINIT() : base(PacketHeader.CZ_NOTIFY_ACTORINIT, SIZE) { }

    public override void Read(BinaryReader reader) { /* no body */ }

    public static CZ_NOTIFY_ACTORINIT Create(BinaryReader reader)
    {
        var packet = new CZ_NOTIFY_ACTORINIT();
        packet.Read(reader);
        return packet;
    }
}
