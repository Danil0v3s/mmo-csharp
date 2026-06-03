using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Quest;
using Map.Server.Session;

namespace Map.Server.Handlers.Quest;

/// <summary>
/// Client toggles a quest's tracked (active/inactive) state in the quest window. rAthena
/// <c>clif_parse_questStateAck</c> (clif.cpp, 0x02b6) → <c>quest_update_status</c>. <c>active != 0</c>
/// maps to <c>Q_ACTIVE</c>, else <c>Q_INACTIVE</c>; the service flips the state, confirms via
/// <c>ZC_ACTIVE_QUEST</c>, and immediately persists (chrif_save parity).
/// </summary>
[PacketHandler(PacketHeader.CZ_ACTIVE_QUEST)]
public class ActiveQuestHandler(
    IEntityRegistry registry,
    IQuestService quests,
    ILogger<ActiveQuestHandler> logger
) : IPacketHandler<MapSessionData, CZ_ACTIVE_QUEST>
{
    // rAthena e_quest_state: Q_INACTIVE = 0, Q_ACTIVE = 1.
    private const byte QInactive = 0, QActive = 1;

    public Task HandleAsync(MapSessionData session, CZ_ACTIVE_QUEST packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        var status = packet.Active != 0 ? QActive : QInactive;
        if (quests.UpdateStatus(pc, packet.QuestId, status) == 0)
            logger.LogInformation("CZ_ACTIVE_QUEST: char {Char} quest {Quest} -> {State}",
                pc.CharacterId, packet.QuestId, packet.Active != 0 ? "active" : "inactive");

        return Task.CompletedTask;
    }
}
