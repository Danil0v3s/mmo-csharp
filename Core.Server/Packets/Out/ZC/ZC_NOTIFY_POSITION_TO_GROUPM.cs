namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Minimap dot position update for a party member. rAthena
/// <c>clif_party_xy</c> (clif.cpp:8065) for regular position updates
/// and <c>clif_party_xy_remove</c> (clif.cpp:10153) for the dot-clear
/// flavor (xy = -1 / -1, fan-out to same-map party members on logout
/// or party leave). Fixed 10 bytes:
/// <c>0x0107 &lt;AID&gt;.L &lt;x&gt;.W &lt;y&gt;.W</c>.
/// </summary>
public class ZC_NOTIFY_POSITION_TO_GROUPM : OutgoingPacket
{
    private const int SIZE = sizeof(short)
                           + sizeof(uint)    // AID
                           + sizeof(short)   // x
                           + sizeof(short);  // y

    public uint AccountId { get; init; }
    /// <summary>Cell x. Use -1 for dot-remove (rAthena clif.cpp:10160).</summary>
    public short X { get; init; }
    /// <summary>Cell y. Use -1 for dot-remove (rAthena clif.cpp:10161).</summary>
    public short Y { get; init; }

    public ZC_NOTIFY_POSITION_TO_GROUPM() : base(PacketHeader.ZC_NOTIFY_POSITION_TO_GROUPM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(X);
        writer.Write(Y);
    }
}
