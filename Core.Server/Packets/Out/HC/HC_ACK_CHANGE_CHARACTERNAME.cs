namespace Core.Server.Packets.Out.HC;

public class HC_ACK_CHANGE_CHARACTERNAME : OutgoingPacket
{
    private const int SIZE = 6; // packetType (2) + result (4)
    
    public uint Result { get; init; }

    public HC_ACK_CHANGE_CHARACTERNAME() : base(PacketHeader.HC_ACK_CHANGE_CHARACTERNAME, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }

    public override int GetSize()
    {
        return SIZE;
    }
}