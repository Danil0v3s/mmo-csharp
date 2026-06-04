namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Request the cash-shop catalog (sent right after the shop opens). rAthena
/// <c>clif_parse_cashshop_list_request</c> (clif.cpp, 0x08c9). Fixed 2 bytes — header only.
/// </summary>
public class CZ_REQ_CASHSHOP_ITEMLIST : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_REQ_CASHSHOP_ITEMLIST() : base(PacketHeader.CZ_REQ_CASHSHOP_ITEMLIST, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_REQ_CASHSHOP_ITEMLIST Create(BinaryReader reader) => new();
}
