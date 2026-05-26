namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Party invitation result feedback. rAthena
/// <c>clif_party_invite_reply</c> (clif.cpp:7958). Sent to the inviter
/// after the invitee accepts / rejects / times out. Fixed 30 bytes:
/// <c>0x02c5 &lt;nick&gt;.24B &lt;result&gt;.L</c>.
/// <para>Result codes (rAthena <c>e_party_invite_reply</c>, clif.cpp:7947-7957):</para>
/// <list type="bullet">
/// <item><description>0 — char is already in a party (MsgStringTable[80])</description></item>
/// <item><description>1 — invite was rejected (MsgStringTable[81])</description></item>
/// <item><description>2 — invite was accepted (MsgStringTable[82])</description></item>
/// <item><description>3 — party is full (MsgStringTable[83])</description></item>
/// <item><description>4 — char of same account already joined (MsgStringTable[608])</description></item>
/// <item><description>5 — char blocked party invites (MsgStringTable[1324])</description></item>
/// <item><description>7 — char is offline / does not exist (MsgStringTable[71])</description></item>
/// </list>
/// </summary>
public class ZC_PARTY_JOIN_REQ_ACK : OutgoingPacket
{
    private const int NameLength = 24;
    private const int SIZE = sizeof(short) + NameLength + sizeof(int);

    public string CharacterName { get; init; } = string.Empty;
    public int Result { get; init; }

    public ZC_PARTY_JOIN_REQ_ACK() : base(PacketHeader.ZC_PARTY_JOIN_REQ_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.WriteFixedString(CharacterName ?? string.Empty, NameLength);
        writer.Write(Result);
    }
}
