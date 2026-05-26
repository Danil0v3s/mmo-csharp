namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Member-left-party broadcast. rAthena <c>clif_party_withdraw</c>
/// (clif.cpp:8025). Sent to remaining party members when one leaves
/// or is expelled. Fixed 31 bytes:
/// <c>0x0105 &lt;AID&gt;.L &lt;name&gt;.24B &lt;result&gt;.B</c>.
/// <para>Result (rAthena <c>e_party_member_withdraw</c>):</para>
/// <list type="bullet">
/// <item><description>0 — member left voluntarily</description></item>
/// <item><description>1 — member was expelled by the leader</description></item>
/// </list>
/// </summary>
public class ZC_DELETE_MEMBER_FROM_GROUP : OutgoingPacket
{
    private const int NameLength = 24;
    private const int SIZE = sizeof(short)
                           + sizeof(uint)      // AID
                           + NameLength
                           + sizeof(byte);     // result

    public uint AccountId { get; init; }
    public string CharacterName { get; init; } = string.Empty;
    /// <summary>0 = leave, 1 = expel. rAthena <c>e_party_member_withdraw</c>.</summary>
    public byte Result { get; init; }

    public ZC_DELETE_MEMBER_FROM_GROUP() : base(PacketHeader.ZC_DELETE_MEMBER_FROM_GROUP, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.WriteFixedString(CharacterName ?? string.Empty, NameLength);
        writer.Write(Result);
    }
}
