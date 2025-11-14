using System.Text;

namespace Core.Server.Packets.Out.AC;

public class AC_ACCEPT_LOGIN_sub
{
    public uint Ip { get; init; }
    public ushort Port { get; init; }
    public string Name { get; init; }
    public ushort Users { get; init; }
    public ushort Type { get; init; }
    public ushort New { get; init; }
    public byte[] Unknown { get; init; } // 128 bytes
}


[PacketVersion(1)]
public class AC_ACCEPT_LOGIN : OutgoingPacket
{
    public short PacketLength { get; init; }
    public uint LoginId1 { get; init; }
    public uint AID { get; init; }
    public uint LoginId2 { get; init; }
    public uint LastIp { get; init; }
    public string LastLogin { get; init; }
    public byte Sex { get; init; }
    public string Token { get; init; }
    public AC_ACCEPT_LOGIN_sub[] CharServers { get; init; }

    public AC_ACCEPT_LOGIN() : base(PacketHeader.AC_ACCEPT_LOGIN, -1)
    {
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(PacketLength);
        writer.Write(LoginId1);
        writer.Write(AID);
        writer.Write(LoginId2);
        writer.Write(LastIp);
        writer.Write(Encoding.UTF8.GetBytes(LastLogin.PadRight(26, '\0')));
        writer.Write(Sex);
        writer.Write(Encoding.UTF8.GetBytes(Token.PadRight(17, '\0')));
        
        foreach (var server in CharServers)
        {
            writer.Write(server.Ip);
            writer.Write(server.Port);
            writer.Write(Encoding.UTF8.GetBytes(server.Name.PadRight(20, '\0')));
            writer.Write(server.Users);
            writer.Write(server.Type);
            writer.Write(server.New);
            writer.Write(server.Unknown);
        }
    }

    public override int GetSize()
    {
        int baseSize = 2 + 2 + 4 + 4 + 4 + 4 + 26 + 1 + 17; // headers and fixed fields
        int subSize = 4 + 2 + 20 + 2 + 2 + 2 + 128; // per char_server
        int totalSize = baseSize + (CharServers?.Length * subSize ?? 0);
        return totalSize; 
    }
}