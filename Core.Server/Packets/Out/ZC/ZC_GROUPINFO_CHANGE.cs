namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Party share-options broadcast. rAthena <c>clif_party_option</c>
/// (clif.cpp:7987). PACKETVER &gt;= 20090603 variant
/// <c>ZC_REQ_GROUPINFO_CHANGE_V2</c> (<c>0x07d8</c>), 8 bytes:
/// <c>0x07d8 &lt;exp option&gt;.L &lt;item pick rule&gt;.B &lt;item share rule&gt;.B</c>.
/// <para>Exp option (rAthena clif.cpp:7980-7983):</para>
/// <list type="bullet">
/// <item><description>0 — exp sharing disabled</description></item>
/// <item><description>1 — exp sharing enabled</description></item>
/// <item><description>2 — cannot change exp sharing (flag=1 path, SELF target)</description></item>
/// </list>
/// </summary>
public class ZC_GROUPINFO_CHANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short)
                           + sizeof(uint)   // expOption
                           + sizeof(byte)   // pickupRule
                           + sizeof(byte);  // shareRule

    public uint ExpOption { get; init; }
    /// <summary><c>party.item &amp; 1</c> → 1 = item pickup share on.</summary>
    public byte ItemPickupRule { get; init; }
    /// <summary><c>party.item &amp; 2</c> → 1 = MVP / loot share on.</summary>
    public byte ItemShareRule { get; init; }

    public ZC_GROUPINFO_CHANGE() : base(PacketHeader.ZC_GROUPINFO_CHANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ExpOption);
        writer.Write(ItemPickupRule);
        writer.Write(ItemShareRule);
    }
}
