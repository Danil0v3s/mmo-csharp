namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Use skill on target" — rAthena
/// <c>clif_parse_UseSkillToId</c> (clif.cpp:12968). Wire format:
/// <c>0438 &lt;skill lv&gt;.W &lt;skill id&gt;.W &lt;target id&gt;.L</c>
/// Total: 2 (header) + 2 (level) + 2 (id) + 4 (target) = 10 bytes.
/// </summary>
public class CZ_USE_SKILL_TOID : IncomingPacket
{
    private const int SIZE = 10;

    public ushort SkillLevel { get; private set; }
    public ushort SkillId { get; private set; }
    public int TargetId { get; private set; }

    public CZ_USE_SKILL_TOID() : base(PacketHeader.CZ_USE_SKILL_TOID, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        SkillLevel = reader.ReadUInt16();
        SkillId = reader.ReadUInt16();
        TargetId = reader.ReadInt32();
    }

    public static CZ_USE_SKILL_TOID Create(BinaryReader reader)
    {
        var packet = new CZ_USE_SKILL_TOID();
        packet.Read(reader);
        return packet;
    }
}
