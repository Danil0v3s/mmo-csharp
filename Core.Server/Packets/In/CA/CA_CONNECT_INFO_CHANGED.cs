namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_CONNECT_INFO_CHANGED : IncomingPacket
{
    private const int SIZE = 26; // header (2) + name (24)
    
    public string Name { get; internal set; }

    public CA_CONNECT_INFO_CHANGED() : base(PacketHeader.CA_CONNECT_INFO_CHANGED, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Name = reader.ReadFixedString(24); // NAME_LENGTH is 24
    }
}