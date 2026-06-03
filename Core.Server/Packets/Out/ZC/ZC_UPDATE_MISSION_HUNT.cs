namespace Core.Server.Packets.Out.ZC;

/// <summary>One objective row for <see cref="ZC_UPDATE_MISSION_HUNT"/>.</summary>
public readonly record struct MissionObjective(int QuestId, int QuestIndex, short Target, short Current);

/// <summary>
/// Live hunt-count update for an active quest's objectives. rAthena <c>clif_quest_update_objective</c>
/// (clif.cpp, modern 0x09fa form). Variable length:
/// <c>09fa &lt;len&gt;.W &lt;count&gt;.W</c> then per objective
/// <c>&lt;quest id&gt;.L &lt;quest index&gt;.L &lt;target&gt;.W &lt;current&gt;.W</c> (12 bytes each). The quest index is
/// <c>quest_id * 1000 + objectiveIndex</c>.
/// </summary>
public class ZC_UPDATE_MISSION_HUNT : OutgoingPacket
{
    private const int EntrySize = 12;

    public IReadOnlyList<MissionObjective> Objectives { get; init; } = Array.Empty<MissionObjective>();

    public ZC_UPDATE_MISSION_HUNT() : base(PacketHeader.ZC_UPDATE_MISSION_HUNT, -1) { }

    public override int GetSize() => 6 + Objectives.Count * EntrySize; // header(2) + len(2) + count(2) + entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Objectives.Count);
        foreach (var o in Objectives)
        {
            writer.Write(o.QuestId);
            writer.Write(o.QuestIndex);
            writer.Write(o.Target);
            writer.Write(o.Current);
        }
    }
}
