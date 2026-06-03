using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>One objective for <see cref="ZC_ADD_QUEST"/>.</summary>
public sealed class AddQuestObjective
{
    public int QuestIndex { get; init; } // quest_id * 1000 + objectiveIndex
    public int MobId { get; init; }
    public short Current { get; init; }
    public string MobName { get; init; } = string.Empty;
}

/// <summary>
/// Add a quest to the client log (primary). rAthena <c>clif_quest_add</c> (clif.cpp, modern 0x09f9).
/// Fixed 143 bytes: header(2) + quest_id.L + state.B + time-diff.B + 3B gap + time.L + numObj.W, then
/// 3 × 42-byte objective slots: <c>questIndex.L effect.L mob_id.L minLv.W maxLv.W current.W name.24</c>
/// (unused slots zero-padded; <c>MAX_QUEST_OBJECTIVES = 3</c>). The mission counts ride the companion
/// <see cref="ZC_ADD_QUEST_MISSION"/>.
/// </summary>
public class ZC_ADD_QUEST : OutgoingPacket
{
    private const int MaxObjectives = 3;
    private const int ObjSlot = 42;
    private const int NameLength = 24;
    private const int SIZE = 17 + MaxObjectives * ObjSlot; // 143

    public int QuestId { get; init; }
    public byte State { get; init; }
    public byte TimeDiff { get; init; }
    public uint Time { get; init; }
    public IReadOnlyList<AddQuestObjective> Objectives { get; init; } = Array.Empty<AddQuestObjective>();

    public ZC_ADD_QUEST() : base(PacketHeader.ZC_ADD_QUEST, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(QuestId);
        writer.Write(State);
        writer.Write(TimeDiff);
        writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)0); // 3-byte gap (rAthena)
        writer.Write(Time);
        writer.Write((short)Objectives.Count);

        for (var i = 0; i < MaxObjectives; i++)
        {
            if (i < Objectives.Count)
            {
                var o = Objectives[i];
                writer.Write(o.QuestIndex);
                writer.Write(0);                 // effect (race/size/element) — not stored in this catalog
                writer.Write(o.MobId);
                writer.Write((short)0);          // min level
                writer.Write((short)0);          // max level
                writer.Write(o.Current);
                WriteName(writer, o.MobName);
            }
            else
            {
                writer.Write(new byte[ObjSlot]); // zero-padded unused slot
            }
        }
    }

    private static void WriteName(BinaryWriter writer, string name)
    {
        var bytes = Encoding.ASCII.GetBytes(name ?? string.Empty);
        var buf = new byte[NameLength];
        Array.Copy(bytes, buf, Math.Min(bytes.Length, NameLength - 1));
        writer.Write(buf);
    }
}
