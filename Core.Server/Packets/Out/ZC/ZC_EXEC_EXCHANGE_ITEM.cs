namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_tradecompleted</c> (clif.cpp:4838) — trade result.
/// Wire (0x00f0): <c>&lt;result&gt;.B</c> — total 3 bytes.
///
/// <c>result</c>: 0 = success, 1 = failure.
/// </summary>
public class ZC_EXEC_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 3;

    public byte Result { get; init; }

    public ZC_EXEC_EXCHANGE_ITEM() : base(PacketHeader.ZC_EXEC_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
