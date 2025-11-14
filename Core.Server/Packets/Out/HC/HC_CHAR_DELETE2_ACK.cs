namespace Core.Server.Packets.Out.HC;

public class HC_CHAR_DELETE2_ACK : OutgoingPacket
{
    private const int SIZE = 14; // packetType (2) + charId (4) + result (4) + deleteDate (4)
    
    public uint CharId { get; init; }
    public uint Result { get; init; }
    public uint DeleteDate { get; init; }

    public HC_CHAR_DELETE2_ACK() : base(PacketHeader.HC_CHAR_DELETE2_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(CharId);
        writer.Write(Result);
        writer.Write(DeleteDate);
    }
}