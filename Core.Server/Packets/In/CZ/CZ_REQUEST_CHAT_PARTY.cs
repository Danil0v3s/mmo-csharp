namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player sends a party chat line. rAthena
/// <c>clif_parse_PartyMessage</c> (clif.cpp:13948). Variable-length:
/// <c>0108 &lt;len&gt;.W &lt;text&gt;.?B</c> where text is the
/// "&lt;name&gt; : &lt;message&gt;\0" form the client pre-formats.
/// </summary>
public class CZ_REQUEST_CHAT_PARTY : IncomingPacket
{
    public string Text { get; private set; } = string.Empty;

    public CZ_REQUEST_CHAT_PARTY() : base(PacketHeader.CZ_REQUEST_CHAT_PARTY, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength <= 0) return;
        var bytes = reader.ReadBytes(bodyLength);
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        Text = System.Text.Encoding.UTF8.GetString(bytes, 0, len);
    }

    public static CZ_REQUEST_CHAT_PARTY Create(BinaryReader reader)
    {
        var packet = new CZ_REQUEST_CHAT_PARTY();
        packet.Read(reader);
        return packet;
    }
}
