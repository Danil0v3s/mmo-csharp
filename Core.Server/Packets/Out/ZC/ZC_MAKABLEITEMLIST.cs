namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_cooking_list</c> (clif.cpp:14076) — opens the
/// Pharmacy / Cooking / Genetic-cooking dialog with the list of
/// craftable items. Variable-length:
/// <code>
///   0x025a (2) + packetLength (2) + listType (2) + itemId[N] (2*N)
/// </code>
/// <para><c>listType</c> mirrors rAthena <c>e_skill_produce_type</c>
/// (0=cooking, 1=mix, 2=brewing, 3=elementary, 4=potion-pot, 5=mado-
/// mechanic, 6=Qi-poison, …). Used by every produce/cook/brew skill
/// dispatch.</para>
/// </summary>
public class ZC_MAKABLEITEMLIST : OutgoingPacket
{
    public short ListType { get; init; }
    public IReadOnlyList<ushort> ItemIds { get; init; } = Array.Empty<ushort>();

    public ZC_MAKABLEITEMLIST() : base(PacketHeader.ZC_MAKABLEITEMLIST, -1) { }

    public override int GetSize()
    {
        // header + packetLength + listType + itemIds[N]
        return sizeof(short) + sizeof(short) + sizeof(short) + (ItemIds.Count * sizeof(ushort));
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ListType);
        foreach (var id in ItemIds) writer.Write(id);
    }
}
