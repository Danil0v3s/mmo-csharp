namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skillcastcancel</c> (clif.cpp:5973) — clears the client's
/// cast bar / progress wheel when a cast is aborted. Emitted on
/// <c>unit_skillcastcancel</c> (damage interrupt, walk-cancel, explicit stop).
///
/// Wire format (id <c>0x01b9</c>, 6 bytes fixed):
/// <code>
///   0x01b9 (2) + GID (4)   // GID = the casting entity's block id
/// </code>
///
/// The packet carries no skill id — the client simply tears down whatever
/// cast UI it is currently showing for <c>GID</c>.
/// </summary>
public class ZC_SKILL_CAST_CANCEL : OutgoingPacket
{
    private const int SIZE = 6;

    /// <summary>Block id (GID / account-or-game id) of the casting entity.</summary>
    public int Gid { get; init; }

    public ZC_SKILL_CAST_CANCEL() : base(PacketHeader.ZC_SKILL_CAST_CANCEL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Gid);
    }
}
