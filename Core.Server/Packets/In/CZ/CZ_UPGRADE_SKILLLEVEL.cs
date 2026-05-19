namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Spend one skill point to raise a skill. rAthena
/// <c>clif_parse_SkillUp</c> (clif.cpp:12729). Wire:
/// <c>0112 &lt;skill id&gt;.W</c>. Total: 2 + 2 = 4 bytes.
/// </summary>
public class CZ_UPGRADE_SKILLLEVEL : IncomingPacket
{
    private const int SIZE = 4;

    public ushort SkillId { get; private set; }

    public CZ_UPGRADE_SKILLLEVEL() : base(PacketHeader.CZ_UPGRADE_SKILLLEVEL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        SkillId = reader.ReadUInt16();
    }

    public static CZ_UPGRADE_SKILLLEVEL Create(BinaryReader reader)
    {
        var packet = new CZ_UPGRADE_SKILLLEVEL();
        packet.Read(reader);
        return packet;
    }
}
