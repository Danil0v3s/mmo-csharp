namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player's sell basket. rAthena <c>clif_parse_NpcSellListSend</c>
/// (clif.cpp:12319). Wire:
/// <c>00c9 &lt;packet_len&gt;.W { &lt;index&gt;.W &lt;amount&gt;.W }*</c>
/// — entry size = 4 bytes. <c>index</c> is the client-side slot
/// (server_index = index − 2).
/// </summary>
public class CZ_PC_SELL_ITEMLIST : IncomingPacket
{
    public IReadOnlyList<SellEntry> Items { get; private set; } = Array.Empty<SellEntry>();

    public CZ_PC_SELL_ITEMLIST() : base(PacketHeader.CZ_PC_SELL_ITEMLIST, -1) { }

    public override void Read(BinaryReader reader)
    {
        var packetLen = reader.ReadUInt16();
        var entrySize = sizeof(ushort) + sizeof(ushort);
        var bodyLen = packetLen - 4;
        var count = bodyLen / entrySize;
        var list = new List<SellEntry>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new SellEntry(reader.ReadUInt16(), reader.ReadUInt16()));
        }
        Items = list;
    }

    public static CZ_PC_SELL_ITEMLIST Create(BinaryReader reader)
    {
        var packet = new CZ_PC_SELL_ITEMLIST();
        packet.Read(reader);
        return packet;
    }

    public readonly record struct SellEntry(ushort ClientIndex, ushort Amount);
}
