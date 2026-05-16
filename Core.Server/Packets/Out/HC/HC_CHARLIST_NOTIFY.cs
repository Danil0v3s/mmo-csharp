namespace Core.Server.Packets.Out.HC;

/// <summary>
/// "Here's how many character pages the client should request."
/// rAthena <c>clif_charlist_notify</c>: <c>0x09A0 &lt;total characters&gt;.L = 6 bytes</c>.
/// Fixed-length; sent right after the per-server slot summary
/// (<c>HC_BLOCK_CHARACTER</c> 0x082D) on a fresh char-server connect.
/// </summary>
public class HC_CHARLIST_NOTIFY : OutgoingPacket
{
    private const int SIZE = 6; // packetType (2) + TotalPages (4)

    public int TotalPages { get; init; }
    public int CharSlots { get; init; }

    /// <summary>
    /// Parameterless ctor used by <see cref="PacketSizeRegistry"/>'s
    /// reflective discovery — without it the registry skips this type and
    /// the framer treats 0x09A0 as unknown.
    /// </summary>
    public HC_CHARLIST_NOTIFY() : base(PacketHeader.HC_CHARLIST_NOTIFY, SIZE) { }

    /// <summary>
    /// Compat overload for the existing call sites that still pass the
    /// header explicitly (left over from when this class served multiple
    /// id variants). Behaves identically to the default ctor.
    /// </summary>
    public HC_CHARLIST_NOTIFY(PacketHeader header) : base(header, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(TotalPages);
    }
}
