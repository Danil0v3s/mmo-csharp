using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Pet.PetOps;
using Map.Server.Session;

namespace Map.Server.Handlers.Pet;

/// <summary>
/// Player clicked a monster to tame. rAthena <c>clif_parse_CatchPet</c> (clif.cpp, 0x019f) →
/// <c>pet_catch_process_end</c>. Forwards the clicked target to <see cref="IPetOpsService.CatchProcessEnd"/>,
/// which validates the armed catch, rolls against the live mob's HP%, and emits the roulette result.
/// </summary>
[PacketHandler(PacketHeader.CZ_TRYCAPTURE_MONSTER)]
public class PetCaptureHandler(
    IEntityRegistry registry,
    IPetOpsService pets,
    ILogger<PetCaptureHandler> logger
) : IPacketHandler<MapSessionData, CZ_TRYCAPTURE_MONSTER>
{
    public Task HandleAsync(MapSessionData session, CZ_TRYCAPTURE_MONSTER packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        pets.CatchProcessEnd(pc, new EntityId((int)packet.TargetId));
        logger.LogInformation("CZ_TRYCAPTURE_MONSTER: char {Char} → target {Target}", pc.CharacterId, packet.TargetId);
        return Task.CompletedTask;
    }
}
