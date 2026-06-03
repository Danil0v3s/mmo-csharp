namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Confirm a quest's active/inactive state to the client. rAthena <c>clif_quest_update_status</c>
/// (clif.cpp, 0x02b7). Fixed 7 bytes: <c>02b7 &lt;quest id&gt;.L &lt;active&gt;.B</c>. Sent only for
/// non-complete transitions; completion is signalled via <see cref="ZC_DEL_QUEST"/> instead.
/// </summary>
public class ZC_ACTIVE_QUEST : OutgoingPacket
{
    private const int SIZE = 7;

    public int QuestId { get; init; }
    public bool Active { get; init; }

    public ZC_ACTIVE_QUEST() : base(PacketHeader.ZC_ACTIVE_QUEST, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(QuestId);
        writer.Write(Active ? (byte)1 : (byte)0);
    }
}
