namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Close the player's buying store. rAthena <c>clif_parse_ReqCloseBuyingStore</c> (clif.cpp, 0x0815).
/// Fixed 2 bytes — header only.
/// </summary>
public class CZ_REQ_CLOSE_BUYING_STORE : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_REQ_CLOSE_BUYING_STORE() : base(PacketHeader.CZ_REQ_CLOSE_BUYING_STORE, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_REQ_CLOSE_BUYING_STORE Create(BinaryReader reader) => new();
}
