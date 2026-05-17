namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_inventory_expansion_info</c> (clif.cpp:22973). Tells the
/// client how many extra inventory slots the character has beyond the
/// base size — emitted as part of <c>pc_authok</c> right between
/// <c>ZC_AID</c> and <c>ZC_ACCEPT_ENTER</c>, since PACKETVER_RE_NUM >=
/// 20181219. Fixed 4 bytes:
///
/// <code>
///   int16 packetType    2
///   int16 expansionSize 2   (inventory_slots - INVENTORY_BASE_SIZE)
/// </code>
///
/// For a freshly-created character with default 100-slot inventory the
/// expansion size is 0 — that's still the shape rAthena emits on the wire.
/// </summary>
public class ZC_EXTEND_BODYITEM_SIZE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short); // 4

    public short ExpansionSize { get; init; }

    public ZC_EXTEND_BODYITEM_SIZE() : base(PacketHeader.ZC_EXTEND_BODYITEM_SIZE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ExpansionSize);
    }
}
