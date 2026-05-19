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
    /// </summary>
    PetEntity? Summon(PlayerEntity owner, int petClassId, string petName);

    /// <summary>Recall the pet — removes the entity from the world.</summary>
    void Recall(PlayerEntity owner);

    /// <summary>Game-loop pump — hunger decay, intimacy adjustments, runaway check.</summary>
    void Tick(long nowTick);
}
