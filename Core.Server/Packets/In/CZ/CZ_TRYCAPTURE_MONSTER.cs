namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Attempt to tame the clicked monster. rAthena <c>clif_parse_CatchPet</c> (clif.cpp, 0x019f). Fixed
/// 6 bytes: <c>019f &lt;target id&gt;.L</c>. Sent after the taming item armed the catch
/// (<c>ZC_START_CAPTURE</c> cursor) and the player clicks a monster.
/// </summary>
public class CZ_TRYCAPTURE_MONSTER : IncomingPacket
{
    private const int SIZE = 6;

    public uint TargetId { get; private set; }

    public CZ_TRYCAPTURE_MONSTER() : base(PacketHeader.CZ_TRYCAPTURE_MONSTER, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        TargetId = reader.ReadUInt32();
    }

    public static CZ_TRYCAPTURE_MONSTER Create(BinaryReader reader)
    {
        var packet = new CZ_TRYCAPTURE_MONSTER();
        packet.Read(reader);
        return packet;
    }
}
