namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Close the cash-shop UI. rAthena <c>clif_parse_cashshop_close</c> (clif.cpp, 0x084a). Fixed 2 bytes
/// — header only. Server-side this just clears the open flag.
/// </summary>
public class CZ_REQ_CLOSE_CASHSHOP : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_REQ_CLOSE_CASHSHOP() : base(PacketHeader.CZ_REQ_CLOSE_CASHSHOP, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_REQ_CLOSE_CASHSHOP Create(BinaryReader reader) => new();
}
