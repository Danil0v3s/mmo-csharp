namespace Core.Server.Packets.Out.ZC;

/// <summary>One achievement row for <see cref="ZC_ALL_ACH_LIST"/> / <see cref="ZC_ACH_UPDATE"/>.
/// 50 bytes on the wire: <c>id.L completed.B count[10].L(40) completedTime.L rewarded.B</c>
/// (rAthena <c>clif_achievement_list_all</c> body, MAX_ACHIEVEMENT_OBJECTIVES = 10).</summary>
public sealed class AchListEntry
{
    public uint AchievementId { get; init; }
    public bool Completed { get; init; }
    /// <summary>Per-objective counters; padded/truncated to 10 slots (MAX_ACHIEVEMENT_OBJECTIVES).</summary>
    public IReadOnlyList<int> Counts { get; init; } = Array.Empty<int>();
    /// <summary>Completion epoch (unix seconds); 0 = not complete.</summary>
    public uint CompletedTime { get; init; }
    public bool Rewarded { get; init; }
}

/// <summary>
/// rAthena <c>clif_achievement_list_all</c> ([clif.cpp:21776]). Full achievement progress summary,
/// pushed on login and re-sent after a title reward grant. Variable length:
/// <c>0a23 &lt;packetLength&gt;.W &lt;count&gt;.L &lt;total_score&gt;.L &lt;level&gt;.W &lt;exp&gt;.L &lt;expNext&gt;.L</c>
/// (22-byte header) then 50 bytes per achievement (<see cref="AchListEntry"/>).
/// </summary>
public class ZC_ALL_ACH_LIST : OutgoingPacket
{
    /// <summary>MAX_ACHIEVEMENT_OBJECTIVES — the count[] array is always 10 entries wide.</summary>
    public const int MaxObjectives = 10;
    private const int EntrySize = 50; // id.L + completed.B + count[10].L + completedTime.L + rewarded.B
    private const int HeaderSize = 22; // header.W + len.W + count.L + score.L + level.W + exp.L + expNext.L

    public IReadOnlyList<AchListEntry> Achievements { get; init; } = Array.Empty<AchListEntry>();
    /// <summary>Sum of completed-achievement scores (rAthena <c>achievement_data.total_score</c>).</summary>
    public int TotalScore { get; init; }
    /// <summary>Achievement level (gold circle).</summary>
    public short Level { get; init; }
    /// <summary>Achievement EXP into the current level (left number in bar).</summary>
    public int Exp { get; init; }
    /// <summary>Achievement EXP to next level (right number in bar).</summary>
    public int ExpNext { get; init; }

    public ZC_ALL_ACH_LIST() : base(PacketHeader.ZC_ALL_ACH_LIST, -1) { }

    public override int GetSize() => HeaderSize + Achievements.Count * EntrySize;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Achievements.Count); // count.L at offset 4
        writer.Write(TotalScore);
        writer.Write(Level);
        writer.Write(Exp);
        writer.Write(ExpNext);
        foreach (var a in Achievements)
            WriteEntry(writer, a);
    }

    /// <summary>Shared 50-byte achievement body, also reused by <see cref="ZC_ACH_UPDATE"/>.</summary>
    internal static void WriteEntry(BinaryWriter writer, AchListEntry a)
    {
        writer.Write(a.AchievementId);
        writer.Write((byte)(a.Completed ? 1 : 0));
        for (var j = 0; j < MaxObjectives; j++)
            writer.Write(j < a.Counts.Count ? a.Counts[j] : 0);
        writer.Write(a.CompletedTime);
        writer.Write((byte)(a.Rewarded ? 1 : 0));
    }
}
