namespace Core.Server.Packets.Out.HC;

public class HC_SEND_MAP_DATA : OutgoingPacket
{
    public uint CharId { get; init; }
    public string MapName { get; init; } = string.Empty;
    public uint Ip { get; init; }
    public ushort Port { get; init; }
    public string Domain { get; init; } = string.Empty;

    public HC_SEND_MAP_DATA() : base(PacketHeader.HC_SEND_MAP_DATA, -1) { } // This header might be conditional

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(CharId);
        writer.WriteFixedString(MapName, PacketConstants.MAP_NAME_LENGTH); // mapname[16]
        writer.WriteIpv4ForClient(Ip);
        writer.Write(Port);
        writer.WriteFixedString(Domain, 128);
    }

    public override int GetSize()
    {
        int size = sizeof(short) + sizeof(uint) + PacketConstants.MAP_NAME_LENGTH + sizeof(uint) + sizeof(ushort) + 128; // + domain[128]
        return size;
    }
}
