namespace Core.Server.Packets.Out.HC;

public class HC_CHAR_DELETE2_ACCEPT_ACK : OutgoingPacket
{
    private const int SIZE = 10; // packetType (2) + charId (4) + result (4)
    
    public uint CharId { get; init; }
    public uint Result { get; init; }

    public HC_CHAR_DELETE2_ACCEPT_ACK() : base(PacketHeader.HC_CHAR_DELETE2_ACCEPT_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(CharId);
        writer.Write(Result);
    }
}