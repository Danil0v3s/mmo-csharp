namespace Core.Server.Packets.Out.HC;

public class HC_REFUSE_MAKECHAR : OutgoingPacket
{
    private const int SIZE = 3; // packetType (2) + error (1)

    public byte Error { get; init; }

    public HC_REFUSE_MAKECHAR() : base(PacketHeader.HC_REFUSE_MAKECHAR, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Error);
    }
}
