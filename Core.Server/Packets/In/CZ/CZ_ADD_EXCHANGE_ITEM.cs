namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Add an item or zeny to the trade window. rAthena
/// <c>clif_parse_TradeAddItem</c> (clif.cpp:12511). Wire:
/// <c>00e8 &lt;index&gt;.W &lt;amount&gt;.L</c> — total 2 + 2 + 4 = 8 bytes.
///
/// <see cref="Index"/> == 0 → zeny add (amount field is zeny count).
/// Otherwise: client_index — server slot = Index − 2 (rAthena
/// <c>server_index</c>).
/// </summary>
public class CZ_ADD_EXCHANGE_ITEM : IncomingPacket
{
    private const int SIZE = 8;

    public ushort Index { get; private set; }
    public int Amount { get; private set; }

    public CZ_ADD_EXCHANGE_ITEM() : base(PacketHeader.CZ_ADD_EXCHANGE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Index = reader.ReadUInt16();
        Amount = reader.ReadInt32();
    }

    public static CZ_ADD_EXCHANGE_ITEM Create(BinaryReader reader)
    {
        var packet = new CZ_ADD_EXCHANGE_ITEM();
        packet.Read(reader);
        return packet;
    }
}
