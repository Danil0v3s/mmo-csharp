using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Services.Intif;
using Map.Server.Session;

namespace Map.Server.Handlers.Achievement;

/// <summary>
/// Client claims an achievement's completion reward. rAthena
/// <c>clif_parse_AchievementCheckReward</c> ([clif.cpp:21849], 0x0a25). The parse handler first flushes
/// the pending achievement save (<c>intif_achievement_save</c>) so the rewarded flag is durable, then
/// runs <c>achievement_check_reward</c> — which validates the achievement is completed + not yet
/// rewarded, grants the item/title, and replies with <c>ZC_REQ_ACH_REWARD_ACK</c> (or re-sends
/// <c>ZC_ALL_ACH_LIST</c> when the reward carries a title). The service owns all gating + emits.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_ACH_REWARD)]
public class AchievementCheckRewardHandler(
    IEntityRegistry registry,
    IAchievementService achievements,
    IIntifService intif,
    ILogger<AchievementCheckRewardHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_ACH_REWARD>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_ACH_REWARD packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        // rAthena: if the achievement log has unsaved changes, persist before claiming so the
        // rewarded stamp survives even if the claim crosses an autosave boundary.
        intif.AchievementSave(pc);
        achievements.CheckReward(pc, packet.AchievementId);

        logger.LogInformation("CZ_REQ_ACH_REWARD: char {Char} claim achievement {Ach}",
            pc.CharacterId, packet.AchievementId);
        return Task.CompletedTask;
    }
}
