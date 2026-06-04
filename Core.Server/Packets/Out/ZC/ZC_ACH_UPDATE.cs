namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_achievement_update</c> ([clif.cpp:21818]). Single-achievement progress
/// notification + the running summary header. Fixed 66 bytes:
/// <c>0a24 &lt;total_score&gt;.L &lt;level&gt;.W &lt;exp&gt;.L &lt;expNext&gt;.L</c> (16-byte header) then the
/// 50-byte achievement body (<see cref="AchListEntry"/>). When <see cref="Achievement"/> is null
/// rAthena zeroes the trailing 40 bytes (id + counts + flags) — used for the no-op login refresh.
/// </summary>
public class ZC_ACH_UPDATE : OutgoingPacket
{
    private const int SIZE = 66;

    /// <summary>Sum of completed-achievement scores.</summary>
    public int TotalScore { get; init; }
    public short Level { get; init; }
    public int Exp { get; init; }
    public int ExpNext { get; init; }
    /// <summary>The updated achievement, or null for a header-only refresh (rAthena passes nullptr).</summary>
    public AchListEntry? Achievement { get; init; }

    public ZC_ACH_UPDATE() : base(PacketHeader.ZC_ACH_UPDATE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(TotalScore);
        writer.Write(Level);
        writer.Write(Exp);
        writer.Write(ExpNext);
        if (Achievement != null)
        {
            ZC_ALL_ACH_LIST.WriteEntry(writer, Achievement);
        }
        else
        {
            // rAthena memset(WFIFOP(fd,16), 0, 40) wipes the achievement body (id + count[10] + flags).
            // After the 14-byte body header written above, the remaining body is the full 50-byte
            // achievement slot — write it all as zero so the fixed 66-byte length holds.
            writer.Write(new byte[SIZE - 2 - 14]);
        }
    }
}
