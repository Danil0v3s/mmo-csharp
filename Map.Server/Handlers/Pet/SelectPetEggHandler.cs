using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Pet.PetOps;
using Map.Server.Session;

namespace Map.Server.Handlers.Pet;

/// <summary>
/// The player chose an egg to hatch from the incubator list. rAthena <c>clif_parse_SelectEgg</c>
/// (clif.cpp, 0x01a7) → <c>pet_select_egg</c>. Converts the client index (server index + 2) and asks
/// <see cref="IPetOpsService.SelectEgg"/> to hatch that egg into a live pet.
/// </summary>
[PacketHandler(PacketHeader.CZ_SELECT_PETEGG)]
public class SelectPetEggHandler(
    IEntityRegistry registry,
    IPetOpsService pets,
    ILogger<SelectPetEggHandler> logger
) : IPacketHandler<MapSessionData, CZ_SELECT_PETEGG>
{
    public Task HandleAsync(MapSessionData session, CZ_SELECT_PETEGG packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        var serverSlot = (short)(packet.Index - 2); // client_index → server index
        var rc = pets.SelectEgg(pc, serverSlot);
        logger.LogInformation("CZ_SELECT_PETEGG: char {Char} egg slot {Slot} -> {Rc}", pc.CharacterId, serverSlot, rc);
        return Task.CompletedTask;
    }
}
