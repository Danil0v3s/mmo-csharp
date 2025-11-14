namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_LOGIN3 : IncomingPacket
{
    private const int SIZE = 48; // header (2) + version (4) + username (24) + passwordMD5 (16) + clienttype (1) + clientinfo (1)
    
    public uint Version { get; internal set; }
    public string Username { get; internal set; }
    public byte[] PasswordMD5 { get; internal set; } // 16 bytes
    public byte Clienttype { get; internal set; }
    public byte Clientinfo { get; internal set; }

    public CA_LOGIN3() : base(PacketHeader.CA_LOGIN3, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Version = reader.ReadUInt32();
        Username = reader.ReadFixedString(24); // NAME_LENGTH is 24
        PasswordMD5 = reader.ReadBytes(16);
        Clienttype = reader.ReadByte();
        Clientinfo = reader.ReadByte();
    }
}