namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Accept or decline a trade request. rAthena
/// <c>clif_parse_TradeAck</c> (clif.cpp:12500). Wire:
/// <c>00e6 &lt;result&gt;.B</c> — total 2 + 1 = 3 bytes.
/// Result codes (rAthena <c>e_ack_trade_response</c>):
/// <list type="bullet">
///   <item>3 = accept</item>
///   <item>4 = cancel / decline</item>
/// </list>
/// </summary>
public class CZ_ACK_EXCHANGE_ITEM : IncomingPacket
{
    private const int SIZE = 3;

    public byte Result { get; private set; }

    public CZ_ACK_EXCHANGE_ITEM() : base(PacketHeader.CZ_ACK_EXCHANGE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Result = reader.ReadByte();
    }

    public static CZ_ACK_EXCHANGE_ITEM Create(BinaryReader reader)
    {
        var packet = new CZ_ACK_EXCHANGE_ITEM();
        packet.Read(reader);
        return packet;
    }
}
