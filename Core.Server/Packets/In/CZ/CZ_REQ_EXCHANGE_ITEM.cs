namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Initiate a trade request to a player. rAthena
/// <c>clif_parse_TradeRequest</c> (clif.cpp:12465). Wire:
/// <c>00e4 &lt;account id&gt;.L</c> — total 2 + 4 = 6 bytes.
///
/// rAthena keys the lookup by account_id via <c>map_id2sd</c> (which
/// indexes by both account and char id depending on the call site).
/// Our map server registry is keyed by EntityId = char id, so the
/// handler resolves account → char via session lookup.
/// </summary>
public class CZ_REQ_EXCHANGE_ITEM : IncomingPacket
{
    private const int SIZE = 6;

    public int TargetAccountId { get; private set; }

    public CZ_REQ_EXCHANGE_ITEM() : base(PacketHeader.CZ_REQ_EXCHANGE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        TargetAccountId = reader.ReadInt32();
    }

    public static CZ_REQ_EXCHANGE_ITEM Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_EXCHANGE_ITEM();
        packet.Read(reader);
        return packet;
    }
}
