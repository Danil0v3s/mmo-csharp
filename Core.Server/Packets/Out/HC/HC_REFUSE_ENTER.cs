namespace Core.Server.Packets.Out.HC;

public class HC_REFUSE_ENTER : OutgoingPacket
{
    private const int SIZE = 3; // packetType (2) + errorCode (1)
    
    public byte ErrorCode { get; init; }

    public HC_REFUSE_ENTER() : base(PacketHeader.HC_REFUSE_ENTER, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(ErrorCode);
    }
}