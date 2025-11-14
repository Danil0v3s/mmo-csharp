namespace Core.Server.Packets.Out.HC;

public class HC_NOTIFY_ACCESSIBLE_MAPNAME : OutgoingPacket
{
    public short PacketLength { get; init; }
    public HC_NOTIFY_ACCESSIBLE_MAPNAME_SUB[] Maps { get; init; } = Array.Empty<HC_NOTIFY_ACCESSIBLE_MAPNAME_SUB>();

    public HC_NOTIFY_ACCESSIBLE_MAPNAME() : base(PacketHeader.HC_NOTIFY_ACCESSIBLE_MAPNAME, false) { }

    public override void Write(BinaryWriter writer)
    {
        // Calculate packet length before writing
        short actualLength = (short)GetSize();

        writer.Write((short)Header);
        writer.Write(actualLength); // packetLength

        foreach (var map in Maps)
        {
            map.Write(writer);
        }
    }

    public override int GetSize()
    {
        int size = sizeof(short) + sizeof(short); // packetType + packetLength
        foreach (var map in Maps)
        {
            size += map.GetSize();
        }
        return size;
    }
}