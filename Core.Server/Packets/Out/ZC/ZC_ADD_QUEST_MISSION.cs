namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Add-quest mission counts (companion to <see cref="ZC_ADD_QUEST"/>). rAthena <c>clif_quest_add</c>
/// secondary (clif.cpp, 0x08fe). Variable: <c>08fe &lt;len&gt;.W</c> then per objective
/// <c>&lt;quest index&gt;.L &lt;mob id&gt;.L &lt;target&gt;.W &lt;current&gt;.W</c> (12 bytes each).
/// Reuses <see cref="MissionObjective"/> (questIndex carried in <see cref="MissionObjective.QuestIndex"/>).
/// </summary>
public class ZC_ADD_QUEST_MISSION : OutgoingPacket
{
    private const int EntrySize = 12;

    /// <summary>Per-objective: <c>QuestId</c> field holds the mob id here; <c>QuestIndex</c> the index.</summary>
    public IReadOnlyList<MissionObjective> Objectives { get; init; } = Array.Empty<MissionObjective>();

    public ZC_ADD_QUEST_MISSION() : base(PacketHeader.ZC_ADD_QUEST_MISSION, -1) { }

    public override int GetSize() => 4 + Objectives.Count * EntrySize; // header(2) + len(2) + entries

    public override void Write(BinaryWriter writer)
    {
        foreach (var o in Objectives)
        {
            writer.Write(o.QuestIndex); // quest_id * 1000 + i
            writer.Write(o.QuestId);    // mob id (reusing the field)
            writer.Write(o.Target);
            writer.Write(o.Current);
        }
    }
}
