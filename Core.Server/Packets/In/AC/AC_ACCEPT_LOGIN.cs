namespace Core.Server.Packets.In.AC;

public class AC_ACCEPT_LOGIN_sub
{
    public uint Ip { get; internal set; }
    public ushort Port { get; internal set; }
    public string Name { get; internal set; } = string.Empty;
    public ushort Users { get; internal set; }
    public ushort Type { get; internal set; }
    public ushort New { get; internal set; }
    public byte[] Unknown { get; internal set; } = Array.Empty<byte>();
}

[PacketVersion(1)]
public class AC_ACCEPT_LOGIN : IncomingPacket
{
    public short PacketLength { get; internal set; }
    public uint LoginId1 { get; internal set; }
    public uint AID { get; internal set; }
    public uint LoginId2 { get; internal set; }
    public uint LastIp { get; internal set; }
    public string LastLogin { get; internal set; } = string.Empty;
    public byte Sex { get; internal set; }
    public string Token { get; internal set; } = string.Empty;
    public AC_ACCEPT_LOGIN_sub[] CharServers { get; internal set; } = [];

    public AC_ACCEPT_LOGIN() : base(PacketHeader.AC_ACCEPT_LOGIN, -1)
    {
    }

    public override void Read(BinaryReader reader)
    {
        PacketLength = checked((short)reader.BaseStream.Length);
        
        LoginId1 = reader.ReadUInt32();
        AID = reader.ReadUInt32();
        LoginId2 = reader.ReadUInt32();
        
        reader.BaseStream.Seek(30, SeekOrigin.Current);
        
        Sex = reader.ReadByte();
        
        Token = reader.ReadFixedString(17);

        const int charServerRecordSize = 160;
        int consumed = 4 + 4 + 4 + 4 + 26 + 1 + 17;
        int remaining = ((int)reader.BaseStream.Length - 4) - consumed;
        if (remaining < 0 || remaining % charServerRecordSize != 0)
        {
            throw new InvalidDataException($"Invalid AC_ACCEPT_LOGIN payload size: {PacketLength}");
        }

        int charServerCount = remaining / charServerRecordSize;
        CharServers = new AC_ACCEPT_LOGIN_sub[charServerCount];

        for (int i = 0; i < charServerCount; i++)
        {
            CharServers[i] = new AC_ACCEPT_LOGIN_sub
            {
                Ip = reader.ReadUInt32(),
                Port = reader.ReadUInt16(),
                Name = reader.ReadFixedString(20),
                Users = reader.ReadUInt16(),
                Type = reader.ReadUInt16(),
                New = reader.ReadUInt16(),
                Unknown = reader.ReadBytes(128)
            };
        }
    }
}
