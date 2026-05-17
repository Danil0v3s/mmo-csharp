namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_navigateTo</c> [clif.cpp:21418], PACKETVER ≥ 20111010.
/// Server-initiated navigation pin (used for welcome events / quest
/// waypoints). Fixed 27 bytes:
///
/// <code>
///   0x08e2 packet_id (2) + type (1) + flag (1) + mapName[16] +
///   x (2) + y (2) + mob_id (2) + hideWindow (1)
/// </code>
/// </summary>
public class ZC_NAVIGATION_TO : OutgoingPacket
{
    private const int SIZE = 27;
    private const int MapNameLength = 16;

    public byte NavType { get; init; }
    public byte Flag { get; init; }
    public string MapName { get; init; } = string.Empty;
    public short X { get; init; }
    public short Y { get; init; }
    public short MobId { get; init; }
    public byte HideWindow { get; init; }

    public ZC_NAVIGATION_TO() : base(PacketHeader.ZC_NAVIGATION_TO, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(NavType);
        writer.Write(Flag);
        var bytes = new byte[MapNameLength];
        var src = System.Text.Encoding.ASCII.GetBytes(MapName ?? string.Empty);
        Array.Copy(src, bytes, Math.Min(src.Length, MapNameLength - 1));
        writer.Write(bytes);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(MobId);
        writer.Write(HideWindow);
    }
}
