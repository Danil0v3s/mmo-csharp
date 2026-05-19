namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server confirms an item arrived in storage. rAthena
/// <c>clif_storageitemadded</c> (clif.cpp:4880). Wire (legacy 0x00f4):
/// <c>&lt;index&gt;.W &lt;amount&gt;.L &lt;nameid&gt;.W &lt;identified&gt;.B
/// &lt;damaged&gt;.B &lt;refine&gt;.B &lt;c1&gt;.W &lt;c2&gt;.W
/// &lt;c3&gt;.W &lt;c4&gt;.W</c> — total 2 + 19 = 21 bytes.
///
/// <see cref="ClientIndex"/> = server_index + 1 (storage uses a different
/// 1-based offset than inventory; matches rAthena <c>client_storage_index</c>).
/// </summary>
public class ZC_ADD_ITEM_TO_STORE : OutgoingPacket
{
    private const int SIZE = 2 + sizeof(ushort) + sizeof(int) + sizeof(ushort) + 3 + 4 * sizeof(ushort);

    public ushort ClientIndex { get; init; }
    public int Amount { get; init; }
    public ushort NameId { get; init; }
    public byte Identified { get; init; } = 1;
    public byte Damaged { get; init; }
    public byte Refine { get; init; }
    public ushort Card0 { get; init; }
    public ushort Card1 { get; init; }
    public ushort Card2 { get; init; }
    public ushort Card3 { get; init; }

    public ZC_ADD_ITEM_TO_STORE() : base(PacketHeader.ZC_ADD_ITEM_TO_STORE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ClientIndex);
        writer.Write(Amount);
        writer.Write(NameId);
        writer.Write(Identified);
        writer.Write(Damaged);
        writer.Write(Refine);
        writer.Write(Card0);
        writer.Write(Card1);
        writer.Write(Card2);
        writer.Write(Card3);
    }
}
