namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Cash-shop opened — carries the player's current point balances. rAthena <c>clif_cashshop_open</c>
/// (clif.cpp, <c>PACKET_ZC_SE_CASHSHOP_OPEN</c> — 0x0a2b, the 14-byte cashPoints+kafraPoints+tab
/// layout; the equivalent 0x0b6e opcode collides with HC_REFUSE_MAKECHAR in our global registry). Fixed 14 bytes:
/// <c>0b6e &lt;cashPoints&gt;.L &lt;kafraPoints&gt;.L &lt;tab&gt;.L</c>.
/// </summary>
public class ZC_SE_CASHSHOP_OPEN : OutgoingPacket
{
    private const int SIZE = 2 + 4 + 4 + 4; // 14

    public int CashPoints { get; init; }
    public int KafraPoints { get; init; }
    public int Tab { get; init; }

    public ZC_SE_CASHSHOP_OPEN() : base(PacketHeader.ZC_SE_CASHSHOP_OPEN, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(CashPoints);
        writer.Write(KafraPoints);
        writer.Write(Tab);
    }
}
