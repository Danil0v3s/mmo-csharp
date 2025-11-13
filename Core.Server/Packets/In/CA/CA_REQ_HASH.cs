namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_REQ_HASH : IncomingPacket
{
    public CA_REQ_HASH() : base(PacketHeader.CA_REQ_HASH, isFixedLength: true)
    {
    }

    public override void Read(BinaryReader reader)
    {
        // No body, header only
    }
    
    public override int GetSize()
    {
        // header (2)
        return 2;
    }
}