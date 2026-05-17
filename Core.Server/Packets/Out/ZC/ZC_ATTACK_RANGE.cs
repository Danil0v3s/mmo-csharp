namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_attackrange</c> ([clif.cpp:3594]). Tells the client
/// how far it's allowed to attack. Fixed 4 bytes:
/// <c>0x013a packet_id (2) + range (2)</c>.
/// </summary>
public class ZC_ATTACK_RANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short);

    public short Range { get; init; }

    public ZC_ATTACK_RANGE() : base(PacketHeader.ZC_ATTACK_RANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Range);
    }
}
