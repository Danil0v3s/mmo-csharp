using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Pet status panel. rAthena <c>clif_send_petstatus</c> (clif.cpp, 0x01a2). Fixed 37 bytes (PACKETVER
/// ≥ 20081126 — includes the trailing job/class word):
/// <c>01a2 &lt;name&gt;.24 &lt;renamed&gt;.B &lt;level&gt;.W &lt;hunger&gt;.W &lt;intimacy&gt;.W &lt;accessory id&gt;.W &lt;class&gt;.W</c>.
/// </summary>
public class ZC_PROPERTY_PET : OutgoingPacket
{
    private const int NameLength = 24;
    private const int SIZE = 2 + NameLength + 1 + 2 + 2 + 2 + 2 + 2; // 37

    public string Name { get; init; } = string.Empty;
    public byte Renamed { get; init; }
    public short Level { get; init; }
    public short Hunger { get; init; }
    public short Intimacy { get; init; }
    public short AccessoryId { get; init; }
    public short Class { get; init; }

    public ZC_PROPERTY_PET() : base(PacketHeader.ZC_PROPERTY_PET, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        var name = Encoding.ASCII.GetBytes(Name ?? string.Empty);
        var buf = new byte[NameLength];
        Array.Copy(name, buf, Math.Min(name.Length, NameLength - 1));
        writer.Write(buf);
        writer.Write(Renamed);
        writer.Write(Level);
        writer.Write(Hunger);
        writer.Write(Intimacy);
        writer.Write(AccessoryId);
        writer.Write(Class);
    }
}
