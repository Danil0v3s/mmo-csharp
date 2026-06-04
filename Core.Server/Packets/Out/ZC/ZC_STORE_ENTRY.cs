using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Show a vending stall sign over the vendor. rAthena <c>clif_showvendingboard</c> (clif.cpp, 0x0131).
/// Fixed 86 bytes: <c>0131 &lt;maker AID&gt;.L &lt;store name&gt;.80B</c>. Broadcast to the area (the vendor
/// already knows their own shop is open).
/// </summary>
public class ZC_STORE_ENTRY : OutgoingPacket
{
    private const int NameLength = 80;
    private const int SIZE = 2 + 4 + NameLength; // 86

    public uint MakerAccountId { get; init; }
    public string StoreName { get; init; } = string.Empty;

    public ZC_STORE_ENTRY() : base(PacketHeader.ZC_STORE_ENTRY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(MakerAccountId);
        var name = Encoding.UTF8.GetBytes(StoreName ?? string.Empty);
        var buf = new byte[NameLength];
        Array.Copy(name, buf, Math.Min(name.Length, NameLength - 1));
        writer.Write(buf);
    }
}
