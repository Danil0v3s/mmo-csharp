namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_achievement_reward_ack</c> ([clif.cpp:21866], 0x0a26). Result of an achievement
/// reward claim. Fixed 7 bytes: <c>0a26 &lt;result&gt;.B &lt;achievementID&gt;.L</c>.
/// <c>result == 1</c> = success (no title), <c>result == 0</c> = failure. When the reward carries a
/// title id rAthena re-sends <see cref="ZC_ALL_ACH_LIST"/> instead of this success ack.
/// </summary>
public class ZC_REQ_ACH_REWARD_ACK : OutgoingPacket
{
    private const int SIZE = 7;

    public byte Result { get; init; }
    public int AchievementId { get; init; }

    public ZC_REQ_ACH_REWARD_ACK() : base(PacketHeader.ZC_REQ_ACH_REWARD_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        writer.Write(AchievementId);
    }
}
