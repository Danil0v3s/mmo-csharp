namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Press OK in the trade window. rAthena
/// <c>clif_parse_TradeOk</c> (clif.cpp:12529). Wire: just the 2-byte
/// header <c>00eb</c>.
/// </summary>
public class CZ_CONCLUDE_EXCHANGE_ITEM : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_CONCLUDE_EXCHANGE_ITEM() : base(PacketHeader.CZ_CONCLUDE_EXCHANGE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader) { /* no body */ }

    public static CZ_CONCLUDE_EXCHANGE_ITEM Create(BinaryReader reader) => new();
}
