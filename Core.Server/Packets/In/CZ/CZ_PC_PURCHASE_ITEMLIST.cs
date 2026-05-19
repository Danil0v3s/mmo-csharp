namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player's buy basket. rAthena <c>clif_parse_NpcBuyListSend</c>
/// (clif.cpp:12271). Variable-length wire:
/// <c>00c8 &lt;packet_len&gt;.W { &lt;amount&gt;.W &lt;name_id&gt;.W }*</c>
/// — entry size = 4 bytes.
/// </summary>
public class CZ_PC_PURCHASE_ITEMLIST : IncomingPacket
{
    public IReadOnlyList<BuyEntry> Items { get; private set; } = Array.Empty<BuyEntry>();

    public CZ_PC_PURCHASE_ITEMLIST() : base(PacketHeader.CZ_PC_PURCHASE_ITEMLIST, -1) { }

    public override void Read(BinaryReader reader)
    {
        // Variable: <packet_len>.W is the first field after the header
        // (which is already consumed by the dispatcher).
        var packetLen = reader.ReadUInt16();
        var entrySize = sizeof(ushort) + sizeof(ushort);
        var bodyLen = packetLen - 4; // header(2) + len(2)
        var count = bodyLen / entrySize;
        var list = new List<BuyEntry>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new BuyEntry(reader.ReadUInt16(), reader.ReadUInt16()));
        }
        Items = list;
    }

    public static CZ_PC_PURCHASE_ITEMLIST Create(BinaryReader reader)
    {
        var packet = new CZ_PC_PURCHASE_ITEMLIST();
        packet.Read(reader);
        return packet;
    }

    public readonly record struct BuyEntry(ushort Amount, ushort NameId);
}
