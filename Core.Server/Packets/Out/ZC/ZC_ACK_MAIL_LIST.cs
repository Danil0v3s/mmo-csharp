using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>One inbox row for <see cref="ZC_ACK_MAIL_LIST"/>.</summary>
public sealed class MailListEntry
{
    /// <summary>Mail tab (rAthena <c>mail_inbox_type</c>: 0 normal / 1 account / 2 returned).</summary>
    public byte Type { get; init; }
    public long MailId { get; init; }
    public bool Read { get; init; }
    public bool HasZeny { get; init; }
    public bool HasItems { get; init; }
    public bool IsNpc { get; init; }
    public string SenderName { get; init; } = string.Empty;
    /// <summary>Seconds until scheduled auto-deletion (rAthena fakes 1 year when none).</summary>
    public uint DeletionSeconds { get; init; }
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// RODEX inbox list (PACKETVER ≥ 20170419 variant). rAthena <c>clif_Mail_refreshinbox</c>
/// (clif.cpp:16047, <c>cmd = 0xac2</c>). Variable length:
/// <c>0ac2 &lt;len&gt;.W &lt;unknown=1&gt;.B</c> then per mail:
/// <c>&lt;type&gt;.B &lt;mailID&gt;.Q &lt;read&gt;.B &lt;flags&gt;.B &lt;sender&gt;.24B &lt;deletion&gt;.L &lt;titleLen&gt;.W &lt;title&gt;.titleLen</c>.
/// flags: TEXT 0 | ZENY 2 | ITEM 4 | NPC 8.
/// </summary>
public class ZC_ACK_MAIL_LIST : OutgoingPacket
{
    private const int NameLength = 24;     // rAthena NAME_LENGTH
    private const int MaxTitle = 40;       // rAthena MAIL_TITLE_LENGTH
    private const byte FlagZeny = 0x2, FlagItem = 0x4, FlagNpc = 0x8;

    public IReadOnlyList<MailListEntry> Mails { get; init; } = Array.Empty<MailListEntry>();

    public ZC_ACK_MAIL_LIST() : base(PacketHeader.ZC_ACK_MAIL_LIST, -1) { }

    private static int TitleBytes(string title) => Math.Min(Encoding.ASCII.GetByteCount(title ?? string.Empty), MaxTitle) + 1;

    public override int GetSize()
    {
        // header(2) + length(2) + unknown(1) + sum(41 + titleLen)
        var size = 5;
        foreach (var m in Mails)
            size += 1 + 8 + 1 + 1 + NameLength + 4 + 2 + TitleBytes(m.Title);
        return size;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((byte)1); // "Unknown" — always 1 in rAthena
        foreach (var m in Mails)
        {
            writer.Write(m.Type);
            writer.Write(m.MailId);
            writer.Write((byte)(m.Read ? 1 : 0));
            byte flags = 0;
            if (m.HasZeny) flags |= FlagZeny;
            if (m.HasItems) flags |= FlagItem;
            if (m.IsNpc) flags |= FlagNpc;
            writer.Write(flags);
            WriteFixedString(writer, m.SenderName, NameLength);
            writer.Write(m.DeletionSeconds);
            var titleLen = TitleBytes(m.Title);
            writer.Write((ushort)titleLen);
            WriteNullTerminated(writer, m.Title, titleLen);
        }
    }

    private static void WriteFixedString(BinaryWriter writer, string s, int width)
    {
        var bytes = Encoding.ASCII.GetBytes(s ?? string.Empty);
        var buf = new byte[width];
        Array.Copy(bytes, buf, Math.Min(bytes.Length, width - 1)); // leave a null terminator
        writer.Write(buf);
    }

    private static void WriteNullTerminated(BinaryWriter writer, string s, int totalLen)
    {
        var bytes = Encoding.ASCII.GetBytes(s ?? string.Empty);
        var n = Math.Min(bytes.Length, totalLen - 1);
        writer.Write(bytes, 0, n);
        for (var i = n; i < totalLen; i++) writer.Write((byte)0);
    }
}
