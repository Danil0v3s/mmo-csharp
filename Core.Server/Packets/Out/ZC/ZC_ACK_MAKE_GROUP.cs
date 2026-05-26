namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Result of organising a party. rAthena <c>clif_party_created</c>
/// (clif.cpp:7792). Fixed 3 bytes: <c>0x00fa &lt;result&gt;.B</c>.
/// <para>Result codes (rAthena clif.cpp:7786-7791):</para>
/// <list type="bullet">
/// <item><description>0 — opens party window + MsgStringTable[77] "party successfully organised"</description></item>
/// <item><description>1 — MsgStringTable[78] "party name already exists"</description></item>
/// <item><description>2 — MsgStringTable[79] "already in a party"</description></item>
/// <item><description>3 — cannot organise parties on this map</description></item>
/// </list>
/// </summary>
public class ZC_ACK_MAKE_GROUP : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte);

    public byte Result { get; init; }

    public ZC_ACK_MAKE_GROUP() : base(PacketHeader.ZC_ACK_MAKE_GROUP, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Result);
    }
}
