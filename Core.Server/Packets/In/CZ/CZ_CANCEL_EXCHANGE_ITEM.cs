namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Cancel the trade. rAthena <c>clif_parse_TradeCancel</c>
/// (clif.cpp:12538). Wire: <c>00ed</c> with no body.
/// </summary>
public class CZ_CANCEL_EXCHANGE_ITEM : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_CANCEL_EXCHANGE_ITEM() : base(PacketHeader.CZ_CANCEL_EXCHANGE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_CANCEL_EXCHANGE_ITEM Create(BinaryReader reader) => new();
}
