using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Pet.PetOps;
using Map.Server.Session;

namespace Map.Server.Handlers.Pet;

/// <summary>
/// Rename the active pet. rAthena <c>clif_parse_ChangePetName</c> (clif.cpp, 0x01a5) →
/// <c>pet_change_name</c>. Forwards the requested name to <see cref="IPetOpsService.ChangeName"/>,
/// which validates it, applies it, and re-emits the pet status panel.
/// </summary>
[PacketHandler(PacketHeader.CZ_RENAME_PET)]
public class RenamePetHandler(
    IEntityRegistry registry,
    IPetOpsService pets,
    ILogger<RenamePetHandler> logger
) : IPacketHandler<MapSessionData, CZ_RENAME_PET>
{
    public Task HandleAsync(MapSessionData session, CZ_RENAME_PET packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        var rc = pets.ChangeName(pc, packet.Name);
        logger.LogInformation("CZ_RENAME_PET: char {Char} name '{Name}' -> {Rc}", pc.CharacterId, packet.Name, rc);
        return Task.CompletedTask;
    }
}
