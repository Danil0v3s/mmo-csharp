using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Pet.PetOps;
using Map.Server.Session;

namespace Map.Server.Handlers.Pet;

/// <summary>
/// Pet emotion / act request. rAthena <c>clif_parse_SendEmotion</c> (clif.cpp, 0x01a9) →
/// <c>clif_pet_emotion</c>. Broadcasts the pet's emotion (<see cref="IPetOpsService.Emotion"/> →
/// ZC_PET_ACT) to everyone in view.
/// </summary>
[PacketHandler(PacketHeader.CZ_PET_ACT)]
public class PetActHandler(
    IEntityRegistry registry,
    IPetOpsService pets,
    ILogger<PetActHandler> logger
) : IPacketHandler<MapSessionData, CZ_PET_ACT>
{
    public Task HandleAsync(MapSessionData session, CZ_PET_ACT packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        pets.Emotion(pc, packet.Data);
        return Task.CompletedTask;
    }
}
