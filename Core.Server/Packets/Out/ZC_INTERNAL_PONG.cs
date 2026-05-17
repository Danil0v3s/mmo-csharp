namespace Core.Server.Packets.Out;

/// <summary>
/// Internal health-check response. Fixed 3 bytes on the wire:
///   header (2) + Ready (1).
/// Ready=1 means the server has finished its boot work (DB loads, peer
/// registrations, etc.) and is safe to drive with real client packets;
/// Ready=0 means "still warming up, retry."
/// </summary>
public class ZC_INTERNAL_PONG : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte); // 3

    public byte Ready { get; init; }

    public ZC_INTERNAL_PONG() : base(PacketHeader.ZC_INTERNAL_PONG, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Ready);
    }
}
