namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Result of an NPC-shop sell attempt. rAthena
/// <c>clif_npc_sell_result</c> (clif.cpp:12303). Wire:
/// <c>00cb &lt;result&gt;.B</c> — 3 bytes. 0 = success, 1 = failure.
/// </summary>
public class ZC_PC_SELL_RESULT : OutgoingPacket
{
    private const int SIZE = 3;

    public byte Result { get; init; }

    public ZC_PC_SELL_RESULT() : base(PacketHeader.ZC_PC_SELL_RESULT, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
