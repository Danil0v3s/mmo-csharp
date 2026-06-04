namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Open / reset the auction window. rAthena <c>clif_parse_Auction_cancelreg</c> (clif.cpp, 0x024b).
/// Fixed 4 bytes: <c>024b &lt;type&gt;.W</c> — 0 = open/create (any action in the window), 1 = cancel
/// the register tab (clears the staged item).
/// </summary>
public class CZ_AUCTION_CREATE : IncomingPacket
{
    private const int SIZE = 4;

    public short Type { get; private set; }

    public CZ_AUCTION_CREATE() : base(PacketHeader.CZ_AUCTION_CREATE, SIZE) { }

    public override void Read(BinaryReader reader) => Type = reader.ReadInt16();

    public static CZ_AUCTION_CREATE Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_CREATE();
        p.Read(reader);
        return p;
    }
}
