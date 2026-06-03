using System.Text;

namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: begin writing a mail. rAthena <c>clif_parse_Mail_beginwrite</c> (clif.cpp:16451) +
/// <c>PACKET_CZ_REQ_OPEN_WRITE_MAIL</c>. Wire: <c>0a08 &lt;receiveName&gt;.24B</c> — 26 bytes.
/// </summary>
public class CZ_REQ_OPEN_WRITE_MAIL : IncomingPacket
{
    private const int SIZE = 26;

    public string ReceiveName { get; private set; } = string.Empty;

    public CZ_REQ_OPEN_WRITE_MAIL() : base(PacketHeader.CZ_REQ_OPEN_WRITE_MAIL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ReceiveName = MailWire.ReadFixedString(reader, 24);
    }

    public static CZ_REQ_OPEN_WRITE_MAIL Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_OPEN_WRITE_MAIL();
        packet.Read(reader);
        return packet;
    }
}

/// <summary>Shared RODEX wire-read helpers.</summary>
internal static class MailWire
{
    /// <summary>Read a fixed-width null-terminated ASCII string and advance the full width.</summary>
    public static string ReadFixedString(BinaryReader reader, int width)
    {
        var bytes = reader.ReadBytes(width);
        var n = Array.IndexOf(bytes, (byte)0);
        if (n < 0) n = bytes.Length;
        return Encoding.ASCII.GetString(bytes, 0, n);
    }
}
