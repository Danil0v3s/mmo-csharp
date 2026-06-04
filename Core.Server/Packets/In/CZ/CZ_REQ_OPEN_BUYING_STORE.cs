namespace Core.Server.Packets.In.CZ;

/// <summary>One buying-store offer: <c>Amount</c> of item <c>NameId</c> wanted at <c>Price</c> each.</summary>
public readonly record struct BuyOffer(short NameId, short Amount, int Price);

/// <summary>
/// Open a buying store ("I'll pay X for Y"). rAthena <c>clif_parse_ReqOpenBuyingStore</c> (clif.cpp,
/// 0x0811). Variable: <c>0811 &lt;len&gt;.W &lt;zeny limit&gt;.L &lt;result&gt;.B &lt;store name&gt;.80B
/// { &lt;name id&gt;.W &lt;amount&gt;.W &lt;price&gt;.L }*</c>. The offer list begins at body offset 85
/// (after limit.L + result.B + the 80-byte name); each offer is 8 bytes.
/// </summary>
public class CZ_REQ_OPEN_BUYING_STORE : IncomingPacket
{
    private const int NameLength = 80;
    private const int OfferSize = 8;

    public int ZenyLimit { get; private set; }
    public byte Result { get; private set; }
    public string StoreName { get; private set; } = string.Empty;
    public IReadOnlyList<BuyOffer> Offers { get; private set; } = Array.Empty<BuyOffer>();

    public CZ_REQ_OPEN_BUYING_STORE() : base(PacketHeader.CZ_REQ_OPEN_BUYING_STORE, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength < 5 + NameLength) return;

        ZenyLimit = reader.ReadInt32();
        Result = reader.ReadByte();
        var nameBytes = reader.ReadBytes(NameLength);
        var nul = Array.IndexOf(nameBytes, (byte)0);
        StoreName = System.Text.Encoding.UTF8.GetString(nameBytes, 0, nul < 0 ? NameLength : nul);

        var offerBytes = bodyLength - 5 - NameLength;
        var count = offerBytes / OfferSize;
        var offers = new BuyOffer[count];
        for (var i = 0; i < count; i++)
        {
            var nameId = reader.ReadInt16();
            var amount = reader.ReadInt16();
            var price = reader.ReadInt32();
            offers[i] = new BuyOffer(nameId, amount, price);
        }
        Offers = offers;
    }

    public static CZ_REQ_OPEN_BUYING_STORE Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_OPEN_BUYING_STORE();
        packet.Read(reader);
        return packet;
    }
}
