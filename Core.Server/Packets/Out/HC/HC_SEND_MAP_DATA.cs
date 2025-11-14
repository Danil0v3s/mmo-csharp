namespace Core.Server.Packets.Out.HC;

public class HC_SEND_MAP_DATA : OutgoingPacket
{
    public uint CharId { get; init; }
    public string MapName { get; init; } = string.Empty;
    public uint Ip { get; init; }
    public ushort Port { get; init; }

    public HC_SEND_MAP_DATA() : base(PacketHeader.HC_SEND_MAP_DATA, false) { } // This header might be conditional

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(CharId);
        writer.WriteFixedString(MapName, PacketConstants.MAP_NAME_LENGTH); // mapname[16]
        writer.Write(Ip);
        writer.Write(Port);
        // Additional fields may be needed based on PACKETVER
    }

    public override int GetSize()
    {
        int size = sizeof(short) + sizeof(uint) + PacketConstants.MAP_NAME_LENGTH + sizeof(uint) + sizeof(ushort); // packetType + charId + mapname[16] + ip + port
        // Additional size may be needed based on PACKETVER >= 20170315 (128 bytes unknown)
        return size;
    }
}