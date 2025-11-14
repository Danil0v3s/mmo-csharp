namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_LOGIN : IncomingPacket
{
    private const int SIZE = 55; // header (2) + version (4) + username (24) + password (24) + clienttype (1)
    
    public uint Version { get; internal set; }
    public string Username { get; internal set; }
    public string Password { get; internal set; }
    public byte Clienttype { get; internal set; }

    public CA_LOGIN() : base(PacketHeader.CA_LOGIN, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Version = reader.ReadUInt32();
        Username = reader.ReadFixedString(24); // NAME_LENGTH is 24
        Password = reader.ReadFixedString(24); // NAME_LENGTH is 24
        Clienttype = reader.ReadByte();
    }
}