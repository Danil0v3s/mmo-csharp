using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>One objective row for <see cref="ZC_ALL_QUEST_LIST"/> (self-contained: carries both the
/// live <see cref="Current"/> count and the catalog <see cref="Target"/> plus the mob display name).
/// 44 bytes on the wire.</summary>
public sealed class AllQuestObjective
{
    public int QuestIndex { get; init; } // quest_id * 1000 + objectiveIndex
    public int Effect { get; init; }     // race ? race : (size ? size : (element ? element : 0))
    public int MobId { get; init; }
    public short MinLevel { get; init; }
    public short MaxLevel { get; init; }
    public short Current { get; init; }
    public short Target { get; init; }
    public string MobName { get; init; } = string.Empty;
}

/// <summary>One quest row for <see cref="ZC_ALL_QUEST_LIST"/> (15-byte fixed head + its objectives).</summary>
public sealed class AllQuestEntry
{
    public int QuestId { get; init; }
    public byte State { get; init; }
    public uint TimeDiff { get; init; } // quest_log.time - qi.time
    public uint Time { get; init; }     // quest_log.time
    public IReadOnlyList<AllQuestObjective> Objectives { get; init; } = Array.Empty<AllQuestObjective>();
}

/// <summary>
/// Full quest-log snapshot pushed on login. rAthena <c>clif_quest_send_list</c> (clif.cpp, modern
/// 0x09f8 / PACKETVER ≥ 20150513). Variable length:
/// <c>09f8 &lt;len&gt;.W &lt;count&gt;.L</c> then per quest
/// <c>quest_id.L state.B (time-qi.time).L time.L numObj.W</c> (15 bytes), and per objective (44 bytes)
/// <c>questIndex.L effect.L mob_id.L minLv.W maxLv.W current.W target.W name.24</c>.
/// </summary>
public class ZC_ALL_QUEST_LIST : OutgoingPacket
{
    private const int NameLength = 24;
    private const int QuestHead = 15;   // quest_id.L + state.B + timeDiff.L + time.L + numObj.W
    private const int ObjSize = 44;     // questIndex.L effect.L mob_id.L minLv.W maxLv.W current.W target.W name.24

    public IReadOnlyList<AllQuestEntry> Quests { get; init; } = Array.Empty<AllQuestEntry>();

    public ZC_ALL_QUEST_LIST() : base(PacketHeader.ZC_ALL_QUEST_LIST, -1) { }

    public override int GetSize()
    {
        var size = 8; // header(2) + len(2) + count(4)
        foreach (var q in Quests)
            size += QuestHead + q.Objectives.Count * ObjSize;
        return size;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Quests.Count); // limit.L at offset 4
        foreach (var q in Quests)
        {
            writer.Write(q.QuestId);
            writer.Write(q.State);
            writer.Write(q.TimeDiff);
            writer.Write(q.Time);
            writer.Write((short)q.Objectives.Count);
            foreach (var o in q.Objectives)
            {
                writer.Write(o.QuestIndex);
                writer.Write(o.Effect);
                writer.Write(o.MobId);
                writer.Write(o.MinLevel);
                writer.Write(o.MaxLevel);
                writer.Write(o.Current);
                writer.Write(o.Target);
                WriteName(writer, o.MobName);
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
