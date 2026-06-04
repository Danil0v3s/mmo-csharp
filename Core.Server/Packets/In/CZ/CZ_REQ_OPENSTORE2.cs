namespace Core.Server.Packets.In.CZ;

/// <summary>One offer in <see cref="CZ_REQ_OPENSTORE2"/>: a cart item to sell.</summary>
/// <param name="Index">Cart client index (server cart index + 2).</param>
/// <param name="Amount">Quantity offered.</param>
/// <param name="Price">Per-unit zeny price.</param>
public readonly record struct VendOffer(short Index, short Amount, int Price);

/// <summary>
/// Open a player vending shop from the cart. rAthena <c>clif_parse_OpenVending</c> (clif.cpp, 0x01b2).
/// Variable: <c>01b2 &lt;len&gt;.W &lt;store name&gt;.80B &lt;flag&gt;.B { &lt;index&gt;.W &lt;amount&gt;.W &lt;price&gt;.L }*</c>.
/// The offer list begins at body offset 81 (after the 80-byte name + 1-byte flag); each offer is 8 bytes.
/// </summary>
public class CZ_REQ_OPENSTORE2 : IncomingPacket
{
    private const int NameLength = 80;
    private const int OfferSize = 8;

    public string StoreName { get; private set; } = string.Empty;
    public byte Flag { get; private set; }
    public IReadOnlyList<VendOffer> Offers { get; private set; } = Array.Empty<VendOffer>();

    public CZ_REQ_OPENSTORE2() : base(PacketHeader.CZ_REQ_OPENSTORE2, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength < NameLength + 1) return;

        var nameBytes = reader.ReadBytes(NameLength);
        var nul = Array.IndexOf(nameBytes, (byte)0);
        StoreName = System.Text.Encoding.UTF8.GetString(nameBytes, 0, nul < 0 ? NameLength : nul);
        Flag = reader.ReadByte();

        var offerBytes = bodyLength - NameLength - 1;
        var count = offerBytes / OfferSize;
        var offers = new VendOffer[count];
        for (var i = 0; i < count; i++)
        {
            var index = reader.ReadInt16();
            var amount = reader.ReadInt16();
            var price = reader.ReadInt32();
            offers[i] = new VendOffer(index, amount, price);
        }
        Offers = offers;
    }

    public static CZ_REQ_OPENSTORE2 Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_OPENSTORE2();
        packet.Read(reader);
        return packet;
    }
}
