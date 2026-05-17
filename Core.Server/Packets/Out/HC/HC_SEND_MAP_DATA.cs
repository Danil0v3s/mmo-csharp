namespace Core.Server.Packets.Out.HC;

/// <summary>
/// rAthena <c>clif_send_mapdata</c> / <c>HC_NOTIFY_ZONESVR3</c>. The map
/// handoff packet: 2 + 4 + 16 + 4 + 2 + 128 = 156 bytes, fixed. Declaring
/// it fixed (instead of HasPacketLength=false + Size=-1) lets the
/// PacketSizeRegistry frame it for the replay framer; otherwise the
/// framer treats every byte as trailing/unknown.
/// </summary>
public class HC_SEND_MAP_DATA : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + PacketConstants.MAP_NAME_LENGTH
                             + sizeof(uint) + sizeof(ushort) + 128; // 156

    public uint CharId { get; init; }
    public string MapName { get; init; } = string.Empty;
    public uint Ip { get; init; }
    public ushort Port { get; init; }
    public string Domain { get; init; } = string.Empty;

    public HC_SEND_MAP_DATA() : base(PacketHeader.HC_SEND_MAP_DATA, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(CharId);
        // rAthena suffixes the map name with ".gat" when serializing for the
        // client (clif_send_mapdata writes "<map>.gat" into the 16-byte name
        // slot). The character-info packet does the same; mirror it here so
        // the client sees a consistent format across both code paths.
        var mapName = string.IsNullOrEmpty(MapName) || MapName.EndsWith(".gat", StringComparison.OrdinalIgnoreCase)
            ? MapName
            : MapName + ".gat";
        writer.WriteFixedString(mapName, PacketConstants.MAP_NAME_LENGTH); // mapname[16]
        writer.WriteIpv4ForClient(Ip);
        writer.Write(Port);
        writer.WriteFixedString(Domain, 128);
    }
}
