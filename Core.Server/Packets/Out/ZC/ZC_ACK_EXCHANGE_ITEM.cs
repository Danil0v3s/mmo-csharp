namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_traderesponse</c> (clif.cpp:4720) — reply to a trade
/// request. Wire (legacy 0x00e7): <c>&lt;result&gt;.B</c> — total 3 bytes.
///
/// Result codes (rAthena <c>e_ack_trade_response</c>):
/// <list type="bullet">
///   <item>0 = char too far</item>
///   <item>1 = character does not exist</item>
///   <item>2 = trade failed</item>
///   <item>3 = accept</item>
///   <item>4 = cancel</item>
///   <item>5 = busy</item>
/// </list>
/// </summary>
public class ZC_ACK_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 3;

    public byte Result { get; init; }

    public ZC_ACK_EXCHANGE_ITEM() : base(PacketHeader.ZC_ACK_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
