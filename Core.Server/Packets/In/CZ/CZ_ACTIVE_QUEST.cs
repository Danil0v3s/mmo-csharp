namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client request to toggle a quest's tracked/active state in the quest window. rAthena
/// <c>clif_parse_questStateAck</c> (clif.cpp, 0x02b6). Fixed 7 bytes:
/// <c>02b6 &lt;quest id&gt;.L &lt;active&gt;.B</c>. <c>active != 0</c> → <c>Q_ACTIVE</c>, else <c>Q_INACTIVE</c>.
/// </summary>
public class CZ_ACTIVE_QUEST : IncomingPacket
{
    private const int SIZE = 7;

    public int QuestId { get; private set; }
    public byte Active { get; private set; }

    public CZ_ACTIVE_QUEST() : base(PacketHeader.CZ_ACTIVE_QUEST, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        QuestId = reader.ReadInt32();
        Active = reader.ReadByte();
    }

    public static CZ_ACTIVE_QUEST Create(BinaryReader reader)
    {
        var packet = new CZ_ACTIVE_QUEST();
        packet.Read(reader);
        return packet;
    }
}
