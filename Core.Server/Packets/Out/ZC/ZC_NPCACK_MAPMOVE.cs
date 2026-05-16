namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server-initiated map change (warp scroll, walking into a warp, GM @warp).
/// rAthena <c>clif_changemap</c>. Shape: 0x0091 packet_id (2) + mapName (16) + x (2) + y (2) = 22 bytes.
/// </summary>
public class ZC_NPCACK_MAPMOVE : OutgoingPacket
{
    private const int SIZE = 22;
    private const int MapNameLength = 16;

    public string MapName { get; init; } = string.Empty;
    public short X { get; init; }
    public short Y { get; init; }

    public ZC_NPCACK_MAPMOVE() : base(PacketHeader.ZC_NPCACK_MAPMOVE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        // rAthena sends the map name with the `.gat` extension. The client expects this.
        var fullName = MapName.EndsWith(".gat") ? MapName : MapName + ".gat";
        var bytes = new byte[MapNameLength];
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(fullName);
        Array.Copy(nameBytes, bytes, Math.Min(nameBytes.Length, MapNameLength - 1));
        writer.Write(bytes);
        writer.Write(X);
        writer.Write(Y);
    }
}
