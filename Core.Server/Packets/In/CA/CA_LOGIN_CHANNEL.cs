namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_LOGIN_CHANNEL : IncomingPacket
{
    private const int SIZE = 85; // header (2) + version (4) + username (24) + password (24) + clienttype (1) + ip (16) + mac (13) + is_gravity (1)
    
    public uint Version { get; internal set; }
    public string Username { get; internal set; }
    public string Password { get; internal set; }
    public byte Clienttype { get; internal set; }
    public string Ip { get; internal set; } // 16 bytes
    public string Mac { get; internal set; } // 13 bytes
    public byte IsGravity { get; internal set; }

    public CA_LOGIN_CHANNEL() : base(PacketHeader.CA_LOGIN_CHANNEL, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Version = reader.ReadUInt32();
        Username = reader.ReadFixedString(24); // NAME_LENGTH is 24
        Password = reader.ReadFixedString(24); // NAME_LENGTH is 24
        Clienttype = reader.ReadByte();
        Ip = reader.ReadFixedString(16);
        Mac = reader.ReadFixedString(13);
        IsGravity = reader.ReadByte();
    }
}