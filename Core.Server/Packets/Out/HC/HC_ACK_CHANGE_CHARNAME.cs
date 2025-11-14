namespace Core.Server.Packets.Out.HC;

public class HC_ACK_CHANGE_CHARNAME : OutgoingPacket
{
    private const int SIZE = 4; // packetType (2) + result (2)
    
    public ushort Result { get; init; }

    public HC_ACK_CHANGE_CHARNAME() : base(PacketHeader.HC_ACK_CHANGE_CHARNAME, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }
}