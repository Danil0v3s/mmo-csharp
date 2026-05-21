namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skill_damage</c> ([clif.cpp ~3520]) — emits when a
/// damaging skill lands a hit. Mirrors <c>PACKET_ZC_NOTIFY_SKILL</c>
/// at PACKETVER ≥ 3 (id <c>0x01de</c>, 33 bytes fixed):
///
/// <code>
///   0x01de (2) + skillId (2) + srcAID (4) + targetID (4) + startTime (4) +
///   attackMT (4) + attackedMT (4) + damage (4) + level (2) + count (2) + action (1)
/// </code>
///
/// <list type="bullet">
///   <item><c>attackMT</c> / <c>attackedMT</c>: source amotion +
///         target amotion (used by the client to time the cast → hit →
///         recover animation chain).</item>
///   <item><c>damage</c>: signed 32-bit total. Negative for absorb.</item>
///   <item><c>count</c>: hit count for multi-hit skills (Sonic Blow = 8,
///         Storm Gust = 3, Bowling Bash = 2, Double Strafe = 2).</item>
///   <item><c>action</c>: <see cref="DamageActionType"/> — the client
///         picks an animation off this byte.</item>
/// </list>
/// </summary>
public class ZC_NOTIFY_SKILL : OutgoingPacket
{
    private const int SIZE = 33;

    public ushort SkillId { get; init; }
    public int SrcAid { get; init; }
    public int TargetId { get; init; }
    public uint StartTime { get; init; }
    public int AttackMotion { get; init; }
    public int AttackedMotion { get; init; }
    public int Damage { get; init; }
    public short Level { get; init; } = 1;
    public short HitCount { get; init; } = 1;
    public DamageActionType ActionType { get; init; } = DamageActionType.SkillDamage;

    public ZC_NOTIFY_SKILL() : base(PacketHeader.ZC_NOTIFY_SKILL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(SkillId);
        writer.Write(SrcAid);
        writer.Write(TargetId);
        writer.Write(StartTime);
        writer.Write(AttackMotion);
        writer.Write(AttackedMotion);
        writer.Write(Damage);
        writer.Write(Level);
        writer.Write(HitCount);
        writer.Write((byte)ActionType);
    }
}
