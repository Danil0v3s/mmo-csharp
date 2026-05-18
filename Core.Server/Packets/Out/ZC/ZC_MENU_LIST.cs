namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Open the choice menu with the supplied options." rAthena
/// <c>clif_scriptmenu</c>. Variable-length: 0x00b7 packet_id (2) +
/// packet_len (2) + npcId (4) + menu (?) where menu is a colon-separated
/// ASCII string ending in a single null byte (e.g. <c>"Save:Storage:Cancel\0"</c>).
///
/// The client responds with <see cref="In.CZ.CZ_CHOOSE_MENU"/> carrying
/// the 1-based selection index (or 255 if the user closed the menu).
/// </summary>
public class ZC_MENU_LIST : OutgoingPacket
{
    public uint NpcId { get; init; }
    /// <summary>Colon-joined option string (caller assembles).</summary>
    public string Menu { get; init; } = string.Empty;

    public ZC_MENU_LIST() : base(PacketHeader.ZC_MENU_LIST, -1) { }

    public override bool HasPacketLength => true;

    public override int GetSize()
    {
        var bodyLen = System.Text.Encoding.ASCII.GetByteCount(Menu ?? string.Empty) + 1;
        return 8 + bodyLen;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(NpcId);
        var bytes = System.Text.Encoding.ASCII.GetBytes(Menu ?? string.Empty);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
