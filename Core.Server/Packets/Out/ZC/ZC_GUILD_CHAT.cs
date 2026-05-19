namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server delivers a guild chat line to a member. rAthena
/// <c>clif_guild_message</c>. Wire:
/// <c>017f &lt;len&gt;.W &lt;message&gt;.?B</c>.
/// </summary>
public class ZC_GUILD_CHAT : OutgoingPacket
{
    public string Message { get; init; } = string.Empty;

    public ZC_GUILD_CHAT() : base(PacketHeader.ZC_GUILD_CHAT, -1) { }

    private byte[] MessageBytes() => System.Text.Encoding.UTF8.GetBytes(Message + "\0");

    public override int GetSize() => 2 + 2 + MessageBytes().Length;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(MessageBytes());
    }
}
