using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;

namespace Map.Server.Pet;

/// <summary>
/// Pet → client emit hub. Mirrors rAthena's <c>clif_send_petstatus</c> / <c>clif_send_petdata</c>
/// emitters — one method per wire packet, routed to the owner's session. Kept separate from
/// <see cref="PetService"/> / <see cref="PetOps.PetOpsService"/> so the emit plumbing has a single
/// home (matching <c>IPartyClientService</c>).
/// </summary>
public interface IPetClientService
{
    /// <summary>rAthena <c>clif_send_petstatus</c> (ZC_PROPERTY_PET) — the full pet panel
    /// (name/level/hunger/intimacy/accessory/class) to the owner.</summary>
    void SendPetStatus(PlayerEntity master, PetEntity pet);

    /// <summary>rAthena <c>clif_send_petdata</c> (ZC_CHANGESTATE_PET) — one changed field
    /// (intimacy/hunger/accessory/performance/...) for the pet, to the owner.</summary>
    void SendPetData(PlayerEntity master, PetEntity pet, PetDataType type, int data);
}
