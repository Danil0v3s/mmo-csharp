using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skill_warppoint</c> — destination chooser for
/// AL_WARP / WZ_QUAGMIRE-class skills. Modern variable-length format
/// (PACKETVER_RE ≥ 20170419, id <c>0x011c</c>):
///
/// <code>
///   0x011c (2) + packetLength (2) + skillId (2) +
///   N * { mapName[12] } (12 bytes per map)
/// </code>
///
/// Map names are 12-byte fixed-length ASCII strings padded with
/// NUL (rAthena's <c>MAP_NAME_LENGTH_EXT = 12</c>). The list
/// always carries the player's save_point map first; if skill level
/// ≥ 2 the memo_point[] entries are appended in order, capped at 4
/// total destinations.
/// </summary>
public class ZC_WARPLIST : OutgoingPacket
{
    private const int MapNameLength = 12;

    public ushort SkillId { get; init; }
    public List<string> Maps { get; init; } = new();

    public ZC_WARPLIST() : base(PacketHeader.ZC_WARPLIST, -1) { }

    public override int GetSize() => 2 + 2 + 2 + Maps.Count * MapNameLength;

    public override void Write(BinaryWriter writer)
    {
        // Header + packetLength are written by WritePacket; here we only
        // emit the body (skillId + maps).
        writer.Write(SkillId);
        foreach (var map in Maps)
        {
            var name = map ?? string.Empty;
            var bytes = Encoding.ASCII.GetBytes(name);
            // rAthena truncates to 11 chars + NUL terminator (no overflow).
            var len = Math.Min(bytes.Length, MapNameLength - 1);
            writer.Write(bytes, 0, len);
            // Pad with NULs to the full 12-byte slot.
            for (var pad = len; pad < MapNameLength; pad++)
                writer.Write((byte)0);
        }
    }
}
