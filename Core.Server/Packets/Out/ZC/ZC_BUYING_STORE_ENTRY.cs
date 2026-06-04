using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Show a buying-store sign over the buyer. rAthena <c>clif_buyingstore_entry</c> (clif.cpp, 0x0814).
/// Fixed 86 bytes: <c>0814 &lt;maker AID&gt;.L &lt;store name&gt;.80B</c>. Broadcast to the area.
/// </summary>
public class ZC_BUYING_STORE_ENTRY : OutgoingPacket
{
    private const int NameLength = 80;
    private const int SIZE = 2 + 4 + NameLength; // 86

    public uint MakerAccountId { get; init; }
    public string StoreName { get; init; } = string.Empty;

    public ZC_BUYING_STORE_ENTRY() : base(PacketHeader.ZC_BUYING_STORE_ENTRY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(MakerAccountId);
        var name = Encoding.UTF8.GetBytes(StoreName ?? string.Empty);
        var buf = new byte[NameLength];
        Array.Copy(name, buf, Math.Min(name.Length, NameLength - 1));
        writer.Write(buf);
    }
}
