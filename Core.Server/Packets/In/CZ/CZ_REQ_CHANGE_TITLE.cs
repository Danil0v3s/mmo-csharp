namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client equips an achievement title shown over the character name. rAthena
/// <c>clif_parse_change_title</c> ([clif.cpp:20721], 0x0a2e). Fixed 6 bytes:
/// <c>0a2e &lt;title_id&gt;.L</c>. <c>title_id &lt;= 0</c> clears the title; a non-owned title id is rejected.
/// </summary>
public class CZ_REQ_CHANGE_TITLE : IncomingPacket
{
    private const int SIZE = 6;

    public int TitleId { get; private set; }

    public CZ_REQ_CHANGE_TITLE() : base(PacketHeader.CZ_REQ_CHANGE_TITLE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        TitleId = reader.ReadInt32();
    }

    public static CZ_REQ_CHANGE_TITLE Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_CHANGE_TITLE();
        packet.Read(reader);
        return packet;
    }
}
