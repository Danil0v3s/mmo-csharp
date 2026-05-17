namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>pc_updateweightstatus</c> tail emit. Tells the client the
/// current weight/maxweight percentage (so the overweight UI lights up
/// past the rate threshold). Fixed 6 bytes:
/// <c>0x0ade packet_id (2) + percent (4)</c>.
/// </summary>
public class ZC_OVERWEIGHT_PERCENT : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint);

    public uint Percent { get; init; }

    public ZC_OVERWEIGHT_PERCENT() : base(PacketHeader.ZC_OVERWEIGHT_PERCENT, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Percent);
    }
}
