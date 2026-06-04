namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Result of the auction close/stop request. rAthena <c>clif_Auction_close</c> (clif.cpp). Fixed 4
/// bytes: <c>&lt;result&gt;.W</c> — 0 = ended, 1 = cannot end, 2 = incorrect id. rAthena writes opcode
/// 0x25d here for client-compat; we use the canonical 0x25e (byte-exact opcode deferred to the
/// live-client validation pass).
/// </summary>
public class ZC_AUCTION_ACK_MY_SELL_STOP : OutgoingPacket
{
    private const int SIZE = 4;

    public short Result { get; init; }

    public ZC_AUCTION_ACK_MY_SELL_STOP() : base(PacketHeader.ZC_AUCTION_ACK_MY_SELL_STOP, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
