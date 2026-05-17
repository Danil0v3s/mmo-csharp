namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_quest_show_event</c>. Shows / hides the quest icon
/// over an NPC's head. Fixed 14 bytes:
/// <c>0x0446 packet_id (2) + targetId (4) + x (2) + y (2) + effect (2) + color (2)</c>.
/// </summary>
public class ZC_QUEST_NOTIFY_EFFECT : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(short) * 4;

    public uint TargetId { get; init; }
    public short X { get; init; }
    public short Y { get; init; }
    public short Effect { get; init; }
    public short Color { get; init; }

    public ZC_QUEST_NOTIFY_EFFECT() : base(PacketHeader.ZC_QUEST_NOTIFY_EFFECT, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(TargetId);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Effect);
        writer.Write(Color);
    }
}
