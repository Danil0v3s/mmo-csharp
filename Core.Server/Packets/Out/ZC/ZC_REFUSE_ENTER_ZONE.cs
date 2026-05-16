namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Map auth refused. rAthena <c>clif_authrefuse</c>.
/// Shape: 0x0074 packet_id (2) + errorCode (1) = 3 bytes.
///
/// Error codes:
///   0 = client type mismatch
///   1 = ID mismatch
///   2 = mobile - out of available time
///   3 = mobile - already logged in
///   4 = mobile - waiting state
/// </summary>
public class ZC_REFUSE_ENTER_ZONE : OutgoingPacket
{
    private const int SIZE = 3;

    public byte ErrorCode { get; init; }

    public ZC_REFUSE_ENTER_ZONE() : base(PacketHeader.ZC_REFUSE_ENTER_ZONE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ErrorCode);
    }
}
