namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_REQ_HASH : IncomingPacket
{
    private const int SIZE = 2; // header (2)
    
    public CA_REQ_HASH() : base(PacketHeader.CA_REQ_HASH, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        // No body, header only
    }
}