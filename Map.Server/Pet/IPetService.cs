using Map.Server.Entities;

namespace Map.Server.Pet;

/// <summary>
/// Pet lifecycle owner. Ports the gameplay-side of rAthena pet.cpp:
/// summon from egg, recall to egg, intimacy / hunger ticks, death
/// behavior. Persistence (pet_db rows + char-server IPC) is owned by
/// <see cref="Map.Server.Services.ICharServerIpcService"/>; this
/// service mediates between client packets / GM commands and the
/// runtime pet entity.
/// </summary>
public interface IPetService
{
    /// <summary>
    /// Summon a pet entity for <paramref name="owner"/> at their cell.
    /// Returns the new <see cref="PetEntity"/> or null if the owner
    /// already has an active pet (rAthena lets only one out at a time).
    /// <paramref name="eggItemId"/> is the egg row that hatched the pet
    /// — written to <see cref="PetEntity.EggId"/> so item-script reads
    /// of <c>getpetinfo(PETINFO_EGGID)</c> resolve at recalc time.
    /// </summary>
    PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0);

    /// <summary>Recall the pet — removes the entity from the world.</summary>
    void Recall(PlayerEntity owner);

    /// <summary>Game-loop pump — hunger decay, intimacy adjustments, runaway check.</summary>
    void Tick(long nowTick);

    /// <summary>
    /// T7.2 — serialize the live pet (looked up by persistent pet_id)
    /// into the gRPC <see cref="Core.Server.IPC.PetData"/> shape
    /// consumed by <c>PetSaveAsync</c>. Returns null if no live pet
    /// with that id exists (e.g. recalled to egg).
    /// </summary>
    Core.Server.IPC.PetData? SerializeSnapshot(int petId);
}
