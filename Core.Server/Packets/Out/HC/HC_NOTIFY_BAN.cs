namespace Core.Server.Packets.Out.HC;

public class HC_NOTIFY_BAN : OutgoingPacket
{
    private const int SIZE = 3; // packetType (2) + result (1)
    
    public byte Result { get; init; }

    public HC_NOTIFY_BAN() : base(PacketHeader.SC_NOTIFY_BAN, SIZE) { } // Using SC_NOTIFY_BAN (0x81) as it's the same packet

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }
}