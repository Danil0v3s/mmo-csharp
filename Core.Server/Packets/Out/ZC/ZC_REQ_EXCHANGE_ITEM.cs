namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_traderequest</c> (clif.cpp:4700). Pops the trade
/// confirmation dialog on the target client.
///
/// Wire (legacy variant 0x00e5): <c>nick.24B</c> — total 26 bytes.
/// The 0x01f4 variant adds target id + lvl; we use the legacy form
/// which the DHXJ client accepts.
/// </summary>
public class ZC_REQ_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 2 + 24;
    private const int NameLength = 24;

    /// <summary>Initiator's character name (NUL-padded to 24 bytes).</summary>
    public string RequesterName { get; init; } = string.Empty;

    public ZC_REQ_EXCHANGE_ITEM() : base(PacketHeader.ZC_REQ_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        var buf = new byte[NameLength];
        var src = System.Text.Encoding.ASCII.GetBytes(RequesterName ?? string.Empty);
        Array.Copy(src, buf, Math.Min(src.Length, NameLength - 1));
        writer.Write(buf);
    }
}
