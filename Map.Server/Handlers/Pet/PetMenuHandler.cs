using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Pet.PetOps;
using Map.Server.Session;

namespace Map.Server.Handlers.Pet;

/// <summary>
/// Pet-menu action from the client. rAthena <c>clif_parse_PetMenu</c> (clif.cpp, 0x01a1) →
/// <c>pet_menu</c>. Routes the menu type (0=info, 1=feed, 2=performance, 3=return-to-egg,
/// 4=unequip) to <see cref="IPetOpsService.Menu"/>, which drives the service action + the
/// corresponding ZC pet packet.
/// </summary>
[PacketHandler(PacketHeader.CZ_COMMAND_PET)]
public class PetMenuHandler(
    IEntityRegistry registry,
    IPetOpsService pets,
    ILogger<PetMenuHandler> logger
) : IPacketHandler<MapSessionData, CZ_COMMAND_PET>
{
    public Task HandleAsync(MapSessionData session, CZ_COMMAND_PET packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        var rc = pets.Menu(pc, packet.Type);
        logger.LogInformation("CZ_COMMAND_PET: char {Char} menu {Type} -> {Rc}", pc.CharacterId, packet.Type, rc);
        return Task.CompletedTask;
    }
}
