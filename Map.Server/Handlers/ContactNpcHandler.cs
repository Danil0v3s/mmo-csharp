using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// "Client clicked an NPC." Phase 1: log the click + close the dialog cleanly
/// so the client doesn't hang waiting for a server reply. Phase 2 replaces
/// the log with an actual invocation of the NPC's <c>onClick</c>
/// <see cref="Scripting.Records.ScriptHandle"/> inside Jint, with
/// <c>ctx.mes / ctx.menu / ctx.close</c> resolving to host functions that
/// drive the dialog state machine.
/// </summary>
[PacketHandler(PacketHeader.CZ_CONTACTNPC)]
public class ContactNpcHandler(
    IEntityRegistry entities,
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
        }
        else if (!npc.Hooks.Any)
        {
            logger.LogDebug(
                "NPC click on {Name} (entity {NpcId}) — no hooks registered, ignoring",
                npc.Name, packet.NpcId);
        }
        else
        {
            logger.LogInformation(
                "NPC click on {Name} @ ({X},{Y}) by char {CharId} — dispatch stubbed until Phase 2",
                npc.Name, npc.X, npc.Y, session.CharacterId);
        }

        // Always close the dialog so the client doesn't hang. Phase 2 will
        // gate this on whether the closure actually finished.
        session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = packet.NpcId });
        return Task.CompletedTask;
    }
}
