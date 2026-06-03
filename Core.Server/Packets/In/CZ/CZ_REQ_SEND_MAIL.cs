namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: send the composed mail. rAthena <c>clif_parse_Mail_send</c> (clif.cpp:16784, PACKETVER ≥
/// 20150513) + the 0x0A6E variant. Variable wire (absolute offsets):
/// <c>0a6e &lt;len&gt;.W &lt;receiver&gt;.24B(@4) &lt;sender&gt;.24B(@28) &lt;zeny&gt;.Q(@52) &lt;titleLen&gt;.W(@60)
/// &lt;textLen&gt;.W(@62) &lt;charId&gt;.L(@64) &lt;title&gt;.titleLen(@68) &lt;text&gt;.textLen(@68+titleLen)</c>.
/// </summary>
public class CZ_REQ_SEND_MAIL : IncomingPacket
{
    public string Receiver { get; private set; } = string.Empty;
    public long Zeny { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    public CZ_REQ_SEND_MAIL() : base(PacketHeader.CZ_REQ_SEND_MAIL, -1) { }

    public override void Read(BinaryReader reader)
    {
        reader.ReadUInt16();                                  // packet length (header already consumed)
        Receiver = MailWire.ReadFixedString(reader, 24);
        reader.ReadBytes(24);                                 // sender (unused — server uses the session)
        Zeny = reader.ReadInt64();
        var titleLen = reader.ReadUInt16();
        var textLen = reader.ReadUInt16();
        reader.ReadInt32();                                   // recipient char id (client hint; server re-resolves)
        Title = ReadVar(reader, titleLen);
        Body = ReadVar(reader, textLen);
    }

    private static string ReadVar(BinaryReader reader, int len)
    {
        if (len <= 0) return string.Empty;
        var bytes = reader.ReadBytes(len);
        var n = Array.IndexOf(bytes, (byte)0);
        if (n < 0) n = bytes.Length;
        return System.Text.Encoding.ASCII.GetString(bytes, 0, n);
    }

    public static CZ_REQ_SEND_MAIL Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_SEND_MAIL();
        packet.Read(reader);
        return packet;
    }
}
