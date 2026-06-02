namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Answer to the Warp/Teleport destination chooser. rAthena
/// <c>clif_parse_UseSkillMap</c> (clif.cpp:13131,
/// <c>struct PACKET_CZ_SELECT_WARPPOINT</c>). Fixed 20 bytes:
/// <c>011b &lt;skill id&gt;.W &lt;map name&gt;.16B</c>.
///
/// <para>The map name is the null-padded destination the player picked
/// from <see cref="Out.ZC.ZC_WARPLIST"/>. rAthena strips the
/// <c>.gat</c> extension via <c>mapindex_getmapname</c>; the C# map
/// registry keys on the bare name, so the same trimming applies here.
/// The sentinel <c>"SavePoint"</c> (first chooser entry) survives the
/// trim and is matched downstream in <c>skill_castend_map</c>.</para>
/// </summary>
public class CZ_SELECT_WARPPOINT : IncomingPacket
{
    private const int SIZE = 20;
    private const int MapNameLength = 16;

    public ushort SkillId { get; private set; }
    public string MapName { get; private set; } = string.Empty;

    public CZ_SELECT_WARPPOINT() : base(PacketHeader.CZ_SELECT_WARPPOINT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        SkillId = reader.ReadUInt16();
        var nameBytes = reader.ReadBytes(MapNameLength);
        MapName = TrimMapName(nameBytes);
    }

    /// <summary>
    /// Null-trim then drop the rAthena <c>.gat</c> extension so the name
    /// matches the map registry's bare keys (mirrors
    /// <c>mapindex_getmapname</c>).
    /// </summary>
    private static string TrimMapName(byte[] bytes)
    {
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        var name = System.Text.Encoding.ASCII.GetString(bytes, 0, len).Trim();
        if (name.EndsWith(".gat", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        return name;
    }

    public static CZ_SELECT_WARPPOINT Create(BinaryReader reader)
    {
        var packet = new CZ_SELECT_WARPPOINT();
        packet.Read(reader);
        return packet;
    }
}
