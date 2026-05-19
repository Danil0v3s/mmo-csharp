namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player sends a guild chat line. rAthena
/// <c>clif_parse_GuildMessage</c> (clif.cpp:14565). Variable-length:
/// <c>017e &lt;len&gt;.W &lt;text&gt;.?B</c> where text is
/// "&lt;name&gt; : &lt;message&gt;\0".
/// </summary>
public class CZ_GUILD_CHAT : IncomingPacket
{
    public string Text { get; private set; } = string.Empty;

    public CZ_GUILD_CHAT() : base(PacketHeader.CZ_GUILD_CHAT, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength <= 0) return;
        var bytes = reader.ReadBytes(bodyLength);
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        Text = System.Text.Encoding.UTF8.GetString(bytes, 0, len);
    }

    public static CZ_GUILD_CHAT Create(BinaryReader reader)
    {
        var packet = new CZ_GUILD_CHAT();
        packet.Read(reader);
        return packet;
    }
}
