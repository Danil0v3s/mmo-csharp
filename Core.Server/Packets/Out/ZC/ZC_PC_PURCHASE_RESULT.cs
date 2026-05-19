namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Result of an NPC-shop buy attempt. rAthena
/// <c>clif_npc_buy_result</c> (clif.cpp:12248). Wire:
/// <c>00ca &lt;result&gt;.B</c> — 3 bytes.
///
/// Result codes (rAthena <c>e_purchase_result</c>):
/// <list type="bullet">
///   <item>0 = success</item>
///   <item>1 = not enough zeny</item>
///   <item>2 = overweight</item>
///   <item>3 = too many items / out of capacity</item>
/// </list>
/// </summary>
public class ZC_PC_PURCHASE_RESULT : OutgoingPacket
{
    private const int SIZE = 3;

    public byte Result { get; init; }

    public ZC_PC_PURCHASE_RESULT() : base(PacketHeader.ZC_PC_PURCHASE_RESULT, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
