namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_delitem</c> / <c>ZC_DELETE_ITEM_FROM_BODY</c> (0x07fa, clif.cpp:2911) — tells
/// the owning client to decrement the shown stack at an inventory slot. Sent SELF on every consume
/// (ammo round spent, potion used, item requirement paid) so the count updates immediately instead
/// of waiting for the next full state sync.
///
/// Fixed 8 bytes: <c>header (2) + deleteType (2) + index (2) + amount (2)</c>.
///
/// <para><c>deleteType</c> (the rAthena delete reason): 0 = Normal, 1 = item used for a skill,
/// 2 = refine failed, 3 = material changed, 4 = moved to storage, 5 = moved to cart, 6 = sold.</para>
/// <para><c>index</c> is the client inventory index (server slot index + 2).</para>
/// </summary>
public class ZC_DELETE_ITEM_FROM_BODY : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort);

    public ushort DeleteType { get; init; }
    public ushort Index { get; init; }
    public ushort Amount { get; init; }

    public ZC_DELETE_ITEM_FROM_BODY() : base(PacketHeader.ZC_DELETE_ITEM_FROM_BODY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(DeleteType);
        writer.Write(Index);
        writer.Write(Amount);
    }
}
