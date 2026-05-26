namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Party invitation popup. rAthena <c>clif_party_invite</c>
/// (clif.cpp:7927). Sent to the target of an invite so the client
/// displays the accept / decline modal. Fixed 30 bytes:
/// <c>0x02c6 &lt;party id&gt;.L &lt;party name&gt;.24B</c>.
/// </summary>
public class ZC_PARTY_JOIN_REQ : OutgoingPacket
{
    private const int NameLength = 24;
    private const int SIZE = sizeof(short) + sizeof(uint) + NameLength;

    public uint PartyId { get; init; }
    public string PartyName { get; init; } = string.Empty;

    public ZC_PARTY_JOIN_REQ() : base(PacketHeader.ZC_PARTY_JOIN_REQ, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(PartyId);
        writer.WriteFixedString(PartyName ?? string.Empty, NameLength);
    }
}
