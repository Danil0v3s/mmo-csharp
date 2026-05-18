using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Scripting.Dialog;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// "Client clicked an NPC." Drives the script's <c>onClick</c> generator
/// through <see cref="IDialogDispatcher"/>. If the NPC has no
/// <c>onClick</c>, send <c>ZC_CLOSE_DIALOG</c> immediately so the client
/// doesn't hang.
/// </summary>
[PacketHandler(PacketHeader.CZ_CONTACTNPC)]
public class ContactNpcHandler(
    IEntityRegistry entities,
    IDialogDispatcher dispatcher,
    ILogger<ContactNpcHandler> logger
) : IPacketHandler<MapSessionData, CZ_CONTACTNPC>
{
    public Task HandleAsync(MapSessionData session, CZ_CONTACTNPC packet)
    {
        var npcId = new EntityId((int)packet.NpcId);
        var npc = entities.Get(npcId) as NpcEntity;

        if (npc == null)
        {
            logger.LogDebug(
                "NPC click for unknown entity {NpcId} from char {CharId}",
                packet.NpcId, session.CharacterId);
            session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = packet.NpcId });
            return Task.CompletedTask;
        }

        if (!dispatcher.StartOnClick(session, npc))
        {
            // No onClick hook (or hook invocation failed). Close the dialog
            // cleanly so the client doesn't hang.
            session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = packet.NpcId });
        }
        return Task.CompletedTask;
    }
}
