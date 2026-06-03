namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Remove a quest from the client's quest log. rAthena <c>clif_quest_delete</c> (clif.cpp). Wire:
/// <c>02b4 &lt;quest id&gt;.L</c> — 6 bytes.
/// </summary>
public class ZC_DEL_QUEST : OutgoingPacket
{
    private const int SIZE = 6;

    public int QuestId { get; init; }

    public ZC_DEL_QUEST() : base(PacketHeader.ZC_DEL_QUEST, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(QuestId);
}
