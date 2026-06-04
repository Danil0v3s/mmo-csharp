namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client claims an achievement's completion reward. rAthena
/// <c>clif_parse_AchievementCheckReward</c> ([clif.cpp:21849], 0x0a25). Fixed 6 bytes:
/// <c>0a25 &lt;achievementID&gt;.L</c>.
/// </summary>
public class CZ_REQ_ACH_REWARD : IncomingPacket
{
    private const int SIZE = 6;

    public int AchievementId { get; private set; }

    public CZ_REQ_ACH_REWARD() : base(PacketHeader.CZ_REQ_ACH_REWARD, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        AchievementId = reader.ReadInt32();
    }

    public static CZ_REQ_ACH_REWARD Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_ACH_REWARD();
        packet.Read(reader);
        return packet;
    }
}
