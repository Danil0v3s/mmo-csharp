namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "An item just dropped onto the floor here — animate it." rAthena
/// <c>clif_dropflooritem</c>. Broadcast to viewers in AOI when an item is
/// created on the map (mob drop, player drop, MVP loot pillar).
/// PACKETVER 20211103 shape (PACKET_ZC_ITEM_FALL_ENTRY5):
///
/// <code>
///   0x0add packet_id (2) + ITAID (4) + ITID (4) + type (2) +
///   IsIdentified (1) + xPos (2) + yPos (2) + subX (1) + subY (1) +
///   count (2) + showdropeffect (1) + dropeffectmode (2) = 24 bytes
/// </code>
///
/// <c>type</c> is rAthena's <c>itemtype()</c> classification (weapon, armor,
/// consumable, etc.); we leave it 0 for MS3 first slice until item_db lands.
/// </summary>
public class ZC_ITEM_FALL_ENTRY : OutgoingPacket
{
    private const int SIZE = 24;

    public int EntityId { get; init; }
    public int ItemId { get; init; }
    public short ItemType { get; init; }
    public byte Identified { get; init; } = 1;
    public short X { get; init; }
    public short Y { get; init; }
    public byte SubX { get; init; }
    public byte SubY { get; init; }
    public short Amount { get; init; }
    public byte ShowDropEffect { get; init; }
    public short DropEffectMode { get; init; }

    public ZC_ITEM_FALL_ENTRY() : base(PacketHeader.ZC_ITEM_FALL_ENTRY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(ItemId);
        writer.Write(ItemType);
        writer.Write(Identified);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(SubX);
        writer.Write(SubY);
        writer.Write(Amount);
        writer.Write(ShowDropEffect);
        writer.Write(DropEffectMode);
    }
}
