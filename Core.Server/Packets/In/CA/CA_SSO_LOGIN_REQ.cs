using System.Text;

namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_SSO_LOGIN_REQ : IncomingPacket
{
    public short PacketLength { get; internal set; }
    public uint Version { get; internal set; }
    public byte Clienttype { get; internal set; }
    public string Username { get; internal set; }
    public string Password { get; internal set; } // 27 bytes
    public string Mac { get; internal set; } // 17 bytes
    public string Ip { get; internal set; } // 15 bytes
    public string Token { get; internal set; } // variable length

    public CA_SSO_LOGIN_REQ() : base(PacketHeader.CA_SSO_LOGIN_REQ, -1)
    {
    }

    public override void Read(BinaryReader reader)
    {
        PacketLength = reader.ReadInt16();
        Version = reader.ReadUInt32();
        Clienttype = reader.ReadByte();
        Username = reader.ReadFixedString(24); // NAME_LENGTH is 24
        Password = reader.ReadFixedString(27);
        Mac = reader.ReadFixedString(17);
        Ip = reader.ReadFixedString(15);
        int tokenLength = PacketLength - (2 + 2 + 4 + 1 + 24 + 27 + 17 + 15); // total - headers and fixed fields
        Token = Encoding.UTF8.GetString(reader.ReadBytes(tokenLength));
    }
}