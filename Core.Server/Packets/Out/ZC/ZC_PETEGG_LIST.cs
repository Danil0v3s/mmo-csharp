namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// List of hatchable pet eggs in the bag. rAthena <c>clif_sendegg</c> (clif.cpp, 0x01a6). Variable
/// length: <c>01a6 &lt;len&gt;.W</c> then one <c>&lt;client index&gt;.W</c> per pet-egg inventory slot
/// (client index = server index + 2). The client opens the incubator dialog; the chosen egg comes
/// back as <see cref="In.CZ.CZ_SELECT_PETEGG"/>.
/// </summary>
public class ZC_PETEGG_LIST : OutgoingPacket
{
    /// <summary>Per-egg client inventory indices (server index + 2).</summary>
    public IReadOnlyList<short> ClientIndices { get; init; } = Array.Empty<short>();

    public ZC_PETEGG_LIST() : base(PacketHeader.ZC_PETEGG_LIST, -1) { }

    public override int GetSize() => 4 + ClientIndices.Count * 2; // header(2) + len(2) + entries

    public override void Write(BinaryWriter writer)
    {
        foreach (var idx in ClientIndices)
            writer.Write(idx);
    }
}
