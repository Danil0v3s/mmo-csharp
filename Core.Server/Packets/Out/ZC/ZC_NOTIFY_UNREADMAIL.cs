namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena mail-system reply [clif.cpp:16003]. Sent after the char server
/// returns the mail inbox count; <c>result=0</c> means "no unread mail".
/// Fixed 3 bytes: <c>0x09e7 packet_id (2) + result (1)</c>.
/// </summary>
public class ZC_NOTIFY_UNREADMAIL : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte);

    public byte Result { get; init; }

    public ZC_NOTIFY_UNREADMAIL() : base(PacketHeader.ZC_NOTIFY_UNREADMAIL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Result);
    }
}
