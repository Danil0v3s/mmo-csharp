namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Press Trade to commit. rAthena <c>clif_parse_TradeCommit</c>.
/// Wire: <c>00ef</c> with no body.
/// </summary>
public class CZ_EXEC_EXCHANGE_ITEM : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_EXEC_EXCHANGE_ITEM() : base(PacketHeader.CZ_EXEC_EXCHANGE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_EXEC_EXCHANGE_ITEM Create(BinaryReader reader) => new();
}
