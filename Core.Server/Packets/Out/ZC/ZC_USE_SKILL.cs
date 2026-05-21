namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skill_nodamage</c> ([clif.cpp ~3700]) — emits when a
/// skill cast resolves without a damage frame: status applies, heals,
/// buffs, self-target effects.
///
/// Renewal wire format (PACKETVER ≥ 20130731, id <c>0x09cb</c>,
/// 17 bytes fixed):
/// <code>
///   0x09cb (2) + skillId (2) + level (4) + targetAID (4) + srcAID (4) + result (1)
/// </code>
///
/// The <c>level</c> field is overloaded: for heal-class skills it carries
/// the heal amount (clamped to int32 in renewal); for buff/status casts
/// it carries the actual skill level. <c>result</c> is the success
/// boolean (1 = applied, 0 = blocked / immune / out-of-range).
/// </summary>
public class ZC_USE_SKILL : OutgoingPacket
{
    private const int SIZE = 17;

    public ushort SkillId { get; init; }
    public int Level { get; init; }
    public int TargetAid { get; init; }
    public int SrcAid { get; init; }
    public byte Result { get; init; } = 1;

    public ZC_USE_SKILL() : base(PacketHeader.ZC_USE_SKILL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(SkillId);
        writer.Write(Level);
        writer.Write(TargetAid);
        writer.Write(SrcAid);
        writer.Write(Result);
    }
}
