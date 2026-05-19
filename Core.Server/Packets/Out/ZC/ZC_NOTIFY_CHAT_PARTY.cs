namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server delivers a party chat line to a member. rAthena
/// <c>clif_party_message</c>. Wire:
/// <c>0109 &lt;len&gt;.W &lt;AID&gt;.L &lt;chatMsg&gt;.?B</c>.
/// </summary>
public class ZC_NOTIFY_CHAT_PARTY : OutgoingPacket
{
    public int AccountId { get; init; }
    public string Message { get; init; } = string.Empty;

    public ZC_NOTIFY_CHAT_PARTY() : base(PacketHeader.ZC_NOTIFY_CHAT_PARTY, -1) { }

    private byte[] MessageBytes() => System.Text.Encoding.UTF8.GetBytes(Message + "\0");

    public override int GetSize() => 2 + 2 + sizeof(int) + MessageBytes().Length;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(MessageBytes());
    }
}
