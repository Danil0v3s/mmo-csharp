using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// "Client wants the name of an entity it can see." rAthena
/// <c>clif_parse_SolveCharName</c>. We resolve the entity from the
/// <see cref="IEntityRegistry"/> and reply with the correct ACK_REQNAMEALL
/// variant for its type (NPC / PC / mob).
///
/// This packet is what actually makes the floating name above an NPC
/// (and the name in the dialog header) appear. The name field carried
/// inside <see cref="ZC_NOTIFY_STANDENTRY"/> is ignored by modern
/// clients — they always issue a follow-up CZ_REQNAME instead.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQNAME)]
public class ReqNameHandler(
    IEntityRegistry entities,
    ILogger<ReqNameHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQNAME>
{
    public Task HandleAsync(MapSessionData session, CZ_REQNAME packet)
    {
        var id = new EntityId((int)packet.EntityId);
        var entity = entities.Get(id);

        switch (entity)
        {
            case NpcEntity npc:
                session.EnqueuePacket(new ZC_ACK_REQNAMEALL_NPC
                {
                    Gid = (uint)npc.Id.Value,
                    GroupId = 0,
                    Name = npc.Name,
                    Title = string.Empty,
                });
                break;

            case PlayerEntity pc:
                session.EnqueuePacket(new ZC_ACK_REQNAMEALL
                {
                    Gid = (uint)pc.Id.Value,
                    CharName = pc.Name,
                    // GP-ACHIEVE — the equipped achievement title rides the name block (rAthena
                    // clif_name → titleId). Party / guild / position remain empty until those
                    // social subsystems land.
                    TitleId = (uint)Math.Max(0, pc.TitleId),
                });
                break;

            case MobEntity mob:
                // Mobs use the NPC-shaped ACK in modern PACKETVERs; the
                // client just renders the name above the unit.
                session.EnqueuePacket(new ZC_ACK_REQNAMEALL_NPC
                {
                    Gid = (uint)mob.Id.Value,
                    GroupId = 0,
                    Name = mob.Name,
                    Title = string.Empty,
                });
                break;

            default:
                logger.LogDebug(
                    "CZ_REQNAME for unknown entity {EntityId} (char {CharId})",
                    packet.EntityId, session.CharacterId);
                break;
        }
        return Task.CompletedTask;
    }
}
