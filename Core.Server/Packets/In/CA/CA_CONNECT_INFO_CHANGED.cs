namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_CONNECT_INFO_CHANGED : IncomingPacket
{
    public string Name { get; internal set; }

    public CA_CONNECT_INFO_CHANGED() : base(PacketHeader.CA_CONNECT_INFO_CHANGED, isFixedLength: true)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Name = reader.ReadFixedString(24); // NAME_LENGTH is 24
    }
    
    public override int GetSize()
    {
        // header (2) + name (24)
        return 2 + 24;
    }
}