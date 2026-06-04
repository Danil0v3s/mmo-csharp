namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Register the staged item for auction. rAthena <c>clif_parse_Auction_register</c> (clif.cpp, 0x024d).
/// Fixed 12 bytes: <c>024d &lt;nowMoney&gt;.L &lt;maxMoney&gt;.L &lt;hours&gt;.W</c> — start price, buy-now
/// price, and duration. Uses the item staged by <see cref="CZ_AUCTION_ADD_ITEM"/>.
/// </summary>
public class CZ_AUCTION_ADD : IncomingPacket
{
    private const int SIZE = 12;

    public int NowMoney { get; private set; }
    public int MaxMoney { get; private set; }
    public short Hours { get; private set; }

    public CZ_AUCTION_ADD() : base(PacketHeader.CZ_AUCTION_ADD, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        NowMoney = (int)reader.ReadUInt32();
        MaxMoney = (int)reader.ReadUInt32();
        Hours = (short)reader.ReadUInt16();
    }

    public static CZ_AUCTION_ADD Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_ADD();
        p.Read(reader);
        return p;
    }
}
